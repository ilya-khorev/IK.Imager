using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IK.Imager.Core.Upload;
using Xunit;

namespace IK.Imager.Core.Tests.Upload;

/// <summary>
/// Nothing is listening on port 1, so a connection that gets as far as the socket is refused by the machine.
/// That is what separates "we refused this address" from "the address refused us".
/// </summary>
public class ImageDownloadHandlerTests
{
    private const string LoopbackUrl = "http://127.0.0.1:1/photo.jpg";

    //ImageDownloader follows redirects itself so that every hop is checked again
    [Fact]
    public void Create_ByDefault_DoesNotFollowRedirects()
    {
        using var handler = ImageDownloadHandler.Create();

        Assert.False(handler.AllowAutoRedirect);
    }

    [Fact]
    public async Task Connect_LoopbackAddress_IsRefusedBeforeTheSocket()
    {
        using var client = new HttpClient(ImageDownloadHandler.Create());

        var exception = await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.GetAsync(LoopbackUrl));

        Assert.Contains("not allowed", Flatten(exception));
    }

    //a name resolves before it connects, so the check has to run on what it resolved to
    [Fact]
    public async Task Connect_HostNameThatResolvesToLoopback_IsRefusedBeforeTheSocket()
    {
        using var client = new HttpClient(ImageDownloadHandler.Create());

        var exception = await Assert.ThrowsAnyAsync<HttpRequestException>(() =>
            client.GetAsync("http://localhost:1/photo.jpg"));

        Assert.Contains("not allowed", Flatten(exception));
    }

    [Fact]
    public async Task Connect_LoopbackAddressWhenPrivateAddressesAreAllowed_ReachesTheSocket()
    {
        using var client = new HttpClient(ImageDownloadHandler.Create(allowPrivateAddresses: true));

        var exception = await Assert.ThrowsAnyAsync<HttpRequestException>(() => client.GetAsync(LoopbackUrl));

        Assert.DoesNotContain("not allowed", Flatten(exception));
    }

    private static string Flatten(Exception exception)
    {
        var messages = new StringBuilder();

        for (var current = exception; current != null; current = current.InnerException)
            messages.AppendLine(current.Message);

        return messages.ToString();
    }
}
