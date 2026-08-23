using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace IK.Imager.Cdn.Akamai;

/// <summary>
/// Builds the EG1-HMAC-SHA256 authorization header Akamai expects.
/// </summary>
/// <remarks>
/// Hand written because the official package is a preview from a repository that has been quiet for
/// months, and its last stable release targets .NET Framework 4.0.
///
/// Note the Akamai API reference shows an "Authorization: EdgeGrid ..." sample. That sample is stale -
/// the scheme below is what the service accepts, and it matches Akamai's own reference implementations.
/// </remarks>
public static class EdgeGridSigner
{
    private const string Scheme = "EG1-HMAC-SHA256";

    /// <summary>
    /// Akamai rejects a request whose timestamp is more than 30 seconds away from its own clock.
    /// </summary>
    public const string TimestampFormat = "yyyyMMdd'T'HH:mm:ss+0000";

    /// <summary>
    /// Returns the value of the Authorization header for a request.
    /// </summary>
    /// <param name="settings">Credentials of the API client.</param>
    /// <param name="request">Request to sign. Its uri must be absolute.</param>
    /// <param name="body">Body bytes, or null when there is none. Only signed for POST.</param>
    /// <param name="timestamp">Time the request is sent.</param>
    /// <param name="nonce">A value that is not reused between requests.</param>
    public static string CreateAuthorizationHeader(AkamaiCdnSettings settings, HttpRequestMessage request,
        byte[]? body, DateTimeOffset timestamp, string nonce)
    {
        var formattedTimestamp = timestamp.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);

        //carries no signature yet, and keeps the trailing semicolon - it is signed in this form
        var unsignedHeader = $"{Scheme} client_token={settings.ClientToken};access_token={settings.AccessToken};" +
                             $"timestamp={formattedTimestamp};nonce={nonce};";

        var signingKey = Base64HmacSha256(formattedTimestamp, Encoding.UTF8.GetBytes(settings.ClientSecret));

        var dataToSign = BuildDataToSign(request, body, unsignedHeader);

        //the key is the base64 text of the first hash, not the bytes behind it
        var signature = Base64HmacSha256(dataToSign, Encoding.UTF8.GetBytes(signingKey));

        return $"{unsignedHeader}signature={signature}";
    }

    private static string BuildDataToSign(HttpRequestMessage request, byte[]? body, string unsignedHeader)
    {
        var requestUri = request.RequestUri ?? throw new InvalidOperationException("The request has no uri.");

        //seven tab separated fields. Fast Purge signs no headers, so the fifth is empty and the string
        //ends up with two tabs in a row - which is where hand written signers usually go wrong
        return string.Join("\t",
            request.Method.Method.ToUpperInvariant(),
            requestUri.Scheme.ToLowerInvariant(),
            requestUri.Authority,
            requestUri.AbsolutePath + requestUri.Query,
            string.Empty,
            ContentHash(request.Method, body),
            unsignedHeader);
    }

    //only a POST carrying a body contributes a content hash
    private static string ContentHash(HttpMethod method, byte[]? body) =>
        method == HttpMethod.Post && body is { Length: > 0 }
            ? Convert.ToBase64String(SHA256.HashData(body))
            : string.Empty;

    private static string Base64HmacSha256(string data, byte[] key) =>
        Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data)));
}
