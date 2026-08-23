using System;
using System.Net.Http;
using System.Text;
using IK.Imager.Cdn.Akamai;
using Xunit;

namespace IK.Imager.Cdn.Tests.Akamai;

/// <summary>
/// The expected signatures were produced by Akamai's own reference implementation
/// (akamai/AkamaiOPEN-edgegrid-python) from the inputs below, and pinned here.
///
/// A test over the purger can only see that some Authorization header was attached, never that it is
/// valid - a wrong signer passes that test and fails in production with a 401. Comparing against known
/// good signatures is what actually covers the algorithm.
/// </summary>
public class EdgeGridSignerTests
{
    private const string Host = "akab-testhost.purge.akamaiapis.net";
    private const string PurgeUrl = $"https://{Host}/ccu/v3/delete/url/production";
    private const string Nonce = "9f8e7d6c-5b4a-3210-9876-543210fedcba";
    private const string Body = """{"objects":["https://cdn.example.com/images/abc.jpg"]}""";

    private static readonly DateTimeOffset Timestamp =
        new(2026, 8, 23, 14, 30, 45, TimeSpan.Zero);

    private static readonly AkamaiCdnSettings Settings = new()
    {
        Host = Host,
        ClientToken = "akab-client-token-xxxxxxxxxxxxxxxx",
        ClientSecret = "clientsecret0000000000000000000000000000000=",
        AccessToken = "akab-access-token-xxxxxxxxxxxxxxxx"
    };

    private const string ExpectedPrefix =
        "EG1-HMAC-SHA256 client_token=akab-client-token-xxxxxxxxxxxxxxxx;" +
        "access_token=akab-access-token-xxxxxxxxxxxxxxxx;" +
        "timestamp=20260823T14:30:45+0000;nonce=9f8e7d6c-5b4a-3210-9876-543210fedcba;signature=";

    private static string Sign(HttpMethod method, string url, string? body) =>
        EdgeGridSigner.CreateAuthorizationHeader(Settings, new HttpRequestMessage(method, url),
            body == null ? null : Encoding.UTF8.GetBytes(body), Timestamp, Nonce);

    [Fact]
    public void CreateAuthorizationHeader_PostWithBody_MatchesTheReferenceImplementation()
    {
        Assert.Equal(ExpectedPrefix + "MAqgiN5OfSrApd+mOuFAqtPrWOiHJqZG0AwUvOb5Ne8=",
            Sign(HttpMethod.Post, PurgeUrl, Body));
    }

    /// <summary>
    /// An empty body contributes no content hash, so this signature differs from the one above by more
    /// than the body - it is the field being empty that is under test.
    /// </summary>
    [Fact]
    public void CreateAuthorizationHeader_PostWithoutBody_MatchesTheReferenceImplementation()
    {
        Assert.Equal(ExpectedPrefix + "MJsHVYubQNt36EVrx5AW06pGjfLe/DQbe/lbXR/9nI0=",
            Sign(HttpMethod.Post, PurgeUrl, body: null));
    }

    /// <summary>
    /// Only a POST is hashed, so a GET signs the same way whether or not it carries a body.
    /// </summary>
    [Fact]
    public void CreateAuthorizationHeader_Get_MatchesTheReferenceImplementation()
    {
        Assert.Equal(ExpectedPrefix + "cQRHTebN3uAl91E8THnBASj8BgW+nm4hluocsJk81xo=",
            Sign(HttpMethod.Get, PurgeUrl, body: null));
    }

    [Fact]
    public void CreateAuthorizationHeader_UrlWithAQueryString_SignsTheQueryToo()
    {
        Assert.Equal(ExpectedPrefix + "DXJrjRpUNGJeO9uB4Vp5//rzqgkCQsjTDnNbx7vteFI=",
            Sign(HttpMethod.Post, PurgeUrl + "?a=1&b=2", Body));
    }

    [Fact]
    public void CreateAuthorizationHeader_AnyRequest_KeepsTheUnsignedHeaderAsThePrefix()
    {
        Assert.StartsWith(ExpectedPrefix, Sign(HttpMethod.Post, PurgeUrl, Body));
    }
}
