using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace IK.Imager.Api.OpenApi;

/// <summary>
/// The &lt;summary&gt; of every documented property of the IK.Imager assemblies, read from the XML
/// documentation files next to them.
///
/// Microsoft.AspNetCore.OpenApi bakes the same comments into the assembly at compile time and needs no
/// file at runtime - this exists only for the properties of a form-bound model, which its generator has
/// nowhere to put (see <see cref="FormRequestOperationTransformer"/>).
/// </summary>
internal sealed class XmlPropertyDescriptions
{
    private const string DocumentationFilePattern = "IK.Imager.*.xml";

    private readonly Dictionary<string, string> _summariesByMemberId;

    private XmlPropertyDescriptions(Dictionary<string, string> summariesByMemberId)
    {
        _summariesByMemberId = summariesByMemberId;
    }

    public static XmlPropertyDescriptions LoadFromBaseDirectory()
    {
        var summariesByMemberId = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var documentationFile in Directory.GetFiles(AppContext.BaseDirectory, DocumentationFilePattern, SearchOption.AllDirectories))
        {
            XDocument document;
            try
            {
                document = XDocument.Load(documentationFile);
            }
            catch (XmlException)
            {
                //the pattern is not specific enough to guarantee an XML documentation file
                continue;
            }

            foreach (var member in document.Root?.Element("members")?.Elements("member") ?? [])
            {
                //"P:" is the documentation comment prefix for a property
                if (member.Attribute("name")?.Value is not {} memberId || !memberId.StartsWith("P:", StringComparison.Ordinal))
                    continue;

                if (member.Element("summary")?.Value is not {} summary || string.IsNullOrWhiteSpace(summary))
                    continue;

                summariesByMemberId[memberId] = Normalize(summary);
            }
        }

        return new XmlPropertyDescriptions(summariesByMemberId);
    }

    /// <summary>
    /// Returns the summary documented for <paramref name="propertyName"/> on <paramref name="containerType"/>,
    /// or null when the property carries no documentation.
    /// </summary>
    public string? ForProperty(Type containerType, string propertyName)
    {
        //documentation is keyed by the type that declares the property, which for an inherited property
        //is not the type the request was bound to
        var declaringType = containerType.GetProperty(propertyName)?.DeclaringType ?? containerType;

        return _summariesByMemberId.GetValueOrDefault($"P:{declaringType.FullName}.{propertyName}");
    }

    /// <summary>
    /// Strips the indentation the comment inherits from the source file it was written in.
    /// </summary>
    private static string Normalize(string summary) =>
        string.Join(Environment.NewLine, summary.Split('\n').Select(line => line.Trim())).Trim();
}
