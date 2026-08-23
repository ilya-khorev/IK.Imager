using System;

namespace IK.Imager.Core.Upload;

/// <summary>
/// Strips everything that can carry a credential out of a url before it is logged.
/// </summary>
public static class UrlRedactor
{
    private const string Malformed = "(malformed url)";

    /// <summary>
    /// Keeps the scheme, the host and the path. A caller may hand us a SAS or another pre-signed url whose
    /// credential sits in the query string, and the only check before this point is Uri.IsWellFormedUriString.
    /// Uri.Authority rather than GetLeftPart(UriPartial.Path), because GetLeftPart keeps the userinfo.
    /// </summary>
    public static string Redact(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}"
            : Malformed;
}
