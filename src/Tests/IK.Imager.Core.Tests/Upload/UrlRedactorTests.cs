using IK.Imager.Core.Upload;
using Xunit;

namespace IK.Imager.Core.Tests.Upload;

/// <summary>
/// Upload-by-url takes any absolute url, and the only check before it reaches a log is
/// Uri.IsWellFormedUriString - so a caller can hand us a credential in a query string or in userinfo.
/// </summary>
public class UrlRedactorTests
{
    [Fact]
    public void Redact_SasToken_RemovesTheQueryString()
    {
        var redacted = UrlRedactor.Redact(
            "https://account.blob.core.windows.net/images/photo.jpg?sv=2024-11-04&sig=Zm9vYmFy%2Bc3VwZXI%3D");

        Assert.Equal("https://account.blob.core.windows.net/images/photo.jpg", redacted);
    }

    [Fact]
    public void Redact_PresignedS3Url_RemovesTheQueryString()
    {
        var redacted = UrlRedactor.Redact(
            "https://bucket.s3.amazonaws.com/photo.jpg?X-Amz-Signature=abc123&X-Amz-Credential=AKIA");

        Assert.Equal("https://bucket.s3.amazonaws.com/photo.jpg", redacted);
    }

    //GetLeftPart(UriPartial.Path) keeps the userinfo, which is why the redaction is built from Uri.Authority
    [Fact]
    public void Redact_UrlWithUserInfo_RemovesTheCredentials()
    {
        var redacted = UrlRedactor.Redact("https://user:password@images.test/photo.jpg");

        Assert.Equal("https://images.test/photo.jpg", redacted);
    }

    [Fact]
    public void Redact_UrlWithNonDefaultPort_KeepsThePort()
    {
        var redacted = UrlRedactor.Redact("https://images.test:8443/photo.jpg?sig=secret");

        Assert.Equal("https://images.test:8443/photo.jpg", redacted);
    }

    [Fact]
    public void Redact_PlainUrl_IsUnchanged()
    {
        var redacted = UrlRedactor.Redact("https://images.test/photo.jpg");

        Assert.Equal("https://images.test/photo.jpg", redacted);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("/relative/photo.jpg")]
    [InlineData("")]
    [InlineData(null)]
    public void Redact_MalformedUrl_ReportsItWithoutThrowing(string? url)
    {
        Assert.Equal("(malformed url)", UrlRedactor.Redact(url));
    }
}
