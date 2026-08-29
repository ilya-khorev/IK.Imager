using System.Text.RegularExpressions;

#pragma warning disable 1591
namespace IK.Imager.Api.Validation;

/// <summary>
/// The one charset every caller-visible identifier has to satisfy - the tenant, the collection and the
/// image id alike. All three can end up in a delivery url, and the tenant and the image id are also the two
/// levels of the Cosmos partition key.
///
/// Values are rejected rather than normalised. The point of letting a caller choose an id is that they can
/// predict the url it produces, and silently rewriting the id takes that away.
/// </summary>
internal static partial class IdentifierConstraints
{
    public const int MaxTenantIdLength = 64;

    public const int MinCollectionLength = 3;
    public const int MaxCollectionLength = 30;

    /// <summary>
    /// Cosmos allows 1023 bytes for a document id and Azure allows 1024 characters for a whole blob path,
    /// which also has to fit the tenant, the collection, a unique prefix and the extension.
    /// </summary>
    public const int MaxImageIdLength = 128;

    /// <summary>
    /// Lowercase letters and digits, with dots, underscores and hyphens allowed inside only.
    /// Lowercase is enforced rather than folded: blob paths, urls and Cosmos ids are all case-sensitive, so
    /// accepting 'SKU-1' and 'sku-1' would quietly create two different images.
    /// </summary>
    public const string Pattern = "^[a-z0-9]([a-z0-9._-]*[a-z0-9])?$";

    [GeneratedRegex(Pattern)]
    private static partial Regex Matcher();

    public static bool IsWellFormed(string? value) =>
        !string.IsNullOrEmpty(value)
        && Matcher().IsMatch(value)
        //a run of dots is legal per the pattern but reads as a path traversal in a url
        && !value.Contains("..");
}
