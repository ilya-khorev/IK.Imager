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
    //IsWellFormedUriString as well as TryCreate, because TryCreate alone reads a filesystem path as an
    //implicit file url - "/etc/passwd" on Linux, "C:\dir" on Windows - and would log it as file:///...
    public static string Redact(string? url) =>
        Uri.IsWellFormedUriString(url, UriKind.Absolute) && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}"
            : Malformed;
}
