using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Divergic.Logging.Xunit;
using IK.Imager.Core.Tests.Infrastructure;
using IK.Imager.Core.Upload;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.Upload;

/// <summary>
/// Upload-by-url hands the service a url the caller chose, and the size limit is only checked once the
/// bytes are already in memory - so the download itself is what has to stop an oversized response.
/// </summary>
public class ImageDownloaderTests(ITestOutputHelper output)
{
    private const string ImageUrl = "https://images.test/photo.jpg";

    [Fact]
    public async Task GetMemoryStream_ResponseWithinTheLimit_ReturnsTheBytes()
    {
        var body = Bytes(500);
        var downloader = Downloader(maxSizeBytes: 1000, Responds(body));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.NotNull(imageStream);
        Assert.Equal(0, imageStream.Position);
        Assert.Equal(body, imageStream.ToArray());
    }

    [Fact]
    public async Task GetMemoryStream_ResponseExactlyAtTheLimit_ReturnsTheBytes()
    {
        var body = Bytes(1000);
        var downloader = Downloader(maxSizeBytes: 1000, Responds(body));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.NotNull(imageStream);
        Assert.Equal(1000, imageStream.Length);
    }

    [Fact]
    public async Task GetMemoryStream_ContentLengthAboveTheLimit_ReturnsNullWithoutReadingTheBody()
    {
        var body = new RecordingStream(Bytes(5000));
        var content = new StreamContent(body);
        content.Headers.ContentLength = 5000;
        var downloader = Downloader(maxSizeBytes: 1000, Responds(content));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
        Assert.False(body.WasRead);
    }

    //a Content-Length can be absent or simply wrong, so the copy is the check that has to hold
    [Fact]
    public async Task GetMemoryStream_BodyLargerThanContentLengthClaims_ReturnsNull()
    {
        var content = new StreamContent(new MemoryStream(Bytes(5000)));
        content.Headers.ContentLength = 10;
        var downloader = Downloader(maxSizeBytes: 1000, Responds(content));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
    }

    [Fact]
    public async Task GetMemoryStream_ErrorStatusCode_ReturnsNull()
    {
        var downloader = Downloader(maxSizeBytes: 1000,
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
    }

    //the caller went away, which is not a download the url is to blame for - a null here would reach the
    //client as a 400
    [Fact]
    public async Task GetMemoryStream_CallerCancelled_Throws()
    {
        var downloader = Downloader(maxSizeBytes: 1000, Responds(Bytes(500)));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            downloader.GetMemoryStream(ImageUrl, cancellation.Token));
    }

    //the HttpClient timeout also surfaces as a TaskCanceledException, but with the caller's token unsignalled
    [Fact]
    public async Task GetMemoryStream_HttpClientTimedOut_ReturnsNull()
    {
        var downloader = Downloader(maxSizeBytes: 1000,
            _ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
    }

    private ImageDownloader Downloader(int maxSizeBytes, Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHttpMessageHandler(respond)),
            ImageLimitations.WithMaxSizeBytes(maxSizeBytes),
            output.BuildLoggerFor<ImageDownloader>());

    private static Func<HttpRequestMessage, HttpResponseMessage> Responds(byte[] body) =>
        Responds(new ByteArrayContent(body));

    private static Func<HttpRequestMessage, HttpResponseMessage> Responds(HttpContent content) =>
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

    private static byte[] Bytes(int count) => Enumerable.Range(0, count).Select(x => (byte)x).ToArray();

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    /// <summary>
    /// Reports whether anything read it, so that "the body was never buffered" is assertable rather than
    /// indistinguishable from a body that was read and then discarded.
    /// </summary>
    private sealed class RecordingStream(byte[] content) : MemoryStream(content)
    {
        public bool WasRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            WasRead = true;
            return base.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            WasRead = true;
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
