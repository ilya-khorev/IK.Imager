using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Divergic.Logging.Xunit;
using IK.Imager.Core.Tests.Infrastructure;
using IK.Imager.Core.Upload;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.Upload;

/// <summary>
/// Upload-by-url hands the service a url the caller chose, so the download is where the caller stops being
/// trusted: the size limit is only checked once the bytes are already in memory, and a redirect can point
/// anywhere at all.
/// </summary>
public class ImageDownloaderTests(ITestOutputHelper output)
{
    private const string ImageUrl = "https://images.test/photo.jpg";

    private readonly ICacheLogger<ImageDownloader> _logger = output.BuildLoggerFor<ImageDownloader>();

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
    public async Task GetMemoryStream_NoContentLength_ReturnsTheBytes()
    {
        var body = Bytes(500);
        var downloader = Downloader(maxSizeBytes: 1000, Responds(new StreamContent(new UnknownLengthStream(body))));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.NotNull(imageStream);
        Assert.Equal(body, imageStream.ToArray());
    }

    //a chunked response declares no length at all, which is the case the copy loop exists for
    [Fact]
    public async Task GetMemoryStream_NoContentLengthAndABodyAboveTheLimit_ReturnsNull()
    {
        var content = new StreamContent(new UnknownLengthStream(Bytes(5000)));
        var downloader = Downloader(maxSizeBytes: 1000, Responds(content));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
    }

    //Content-Length only sizes the buffer. A server claiming far more than it sends would otherwise cost
    //that much memory on every request.
    [Fact]
    public async Task GetMemoryStream_ContentLengthFarAboveTheBody_DoesNotPreallocateWhatItClaims()
    {
        var content = new ByteArrayContent(Bytes(10));
        content.Headers.ContentLength = 8_000_000;
        var downloader = Downloader(maxSizeBytes: 10_000_000, Responds(content));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.NotNull(imageStream);
        Assert.Equal(10, imageStream.Length);
        Assert.InRange(imageStream.Capacity, 0, 1024 * 1024);
    }

    [Fact]
    public async Task GetMemoryStream_ErrorStatusCode_ReturnsNullAndReportsNoException()
    {
        var downloader = Downloader(maxSizeBytes: 1000, _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
        //a dead link means the url the caller sent was wrong, not that this service faulted
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.LogLevel);
        Assert.Null(entry.Exception);
        Assert.Contains("404", entry.Message);
    }

    [Fact]
    public async Task GetMemoryStream_Redirected_FollowsTheRedirect()
    {
        var body = Bytes(500);
        var requested = new List<Uri>();
        var downloader = Downloader(maxSizeBytes: 1000, request =>
        {
            requested.Add(request.RequestUri!);
            return requested.Count == 1 ? Redirect("https://cdn.test/photo.jpg") : Ok(body);
        });

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.NotNull(imageStream);
        Assert.Equal(body, imageStream.ToArray());
        Assert.Equal(new Uri("https://cdn.test/photo.jpg"), requested[1]);
    }

    [Fact]
    public async Task GetMemoryStream_RelativeRedirect_ResolvesItAgainstTheUrlThatAnsweredIt()
    {
        var requested = new List<Uri>();
        var downloader = Downloader(maxSizeBytes: 1000, request =>
        {
            requested.Add(request.RequestUri!);
            return requested.Count == 1 ? Redirect("/other.jpg") : Ok(Bytes(500));
        });

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.NotNull(imageStream);
        Assert.Equal(new Uri("https://images.test/other.jpg"), requested[1]);
    }

    [Fact]
    public async Task GetMemoryStream_MoreRedirectsThanAllowed_ReturnsNull()
    {
        var requests = 0;
        var downloader = Downloader(maxSizeBytes: 1000, _ =>
        {
            requests++;
            return Redirect("https://images.test/next.jpg");
        }, maxRedirects: 2);

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
        //the url the caller gave, plus the two redirects it was allowed to follow
        Assert.Equal(3, requests);
    }

    //HttpClient would follow this one on its own, which is how a url that looks harmless reaches a scheme
    //nothing should be reading
    [Fact]
    public async Task GetMemoryStream_RedirectToAnotherScheme_ReturnsNull()
    {
        var requests = 0;
        var downloader = Downloader(maxSizeBytes: 1000, _ =>
        {
            requests++;
            return Redirect("file:///etc/passwd");
        });

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task GetMemoryStream_UrlOfAnotherScheme_ReturnsNullWithoutARequest()
    {
        var requests = 0;
        var downloader = Downloader(maxSizeBytes: 1000, _ =>
        {
            requests++;
            return Ok(Bytes(10));
        });

        var imageStream = await downloader.GetMemoryStream("ftp://images.test/photo.jpg", CancellationToken.None);

        Assert.Null(imageStream);
        Assert.Equal(0, requests);
    }

    [Fact]
    public async Task GetMemoryStream_UrlThatIsNotAbsolute_ReturnsNullWithoutARequest()
    {
        var requests = 0;
        var downloader = Downloader(maxSizeBytes: 1000, _ =>
        {
            requests++;
            return Ok(Bytes(10));
        });

        var imageStream = await downloader.GetMemoryStream("/images/photo.jpg", CancellationToken.None);

        Assert.Null(imageStream);
        Assert.Equal(0, requests);
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

    //the HttpClient timeout also surfaces as a TaskCanceledException, but with the caller token unsignalled
    [Fact]
    public async Task GetMemoryStream_HttpClientTimedOut_ReturnsNull()
    {
        var downloader = Downloader(maxSizeBytes: 1000,
            _ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

        var imageStream = await downloader.GetMemoryStream(ImageUrl, CancellationToken.None);

        Assert.Null(imageStream);
    }

    //only what fetching a url can fail with becomes "the url yielded nothing" and a 400. A bug in this
    //service is a 500.
    [Fact]
    public async Task GetMemoryStream_UnexpectedFailure_IsNotSwallowed()
    {
        var downloader = Downloader(maxSizeBytes: 1000, _ => throw new InvalidOperationException("a bug"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            downloader.GetMemoryStream(ImageUrl, CancellationToken.None));
    }

    private ImageDownloader Downloader(int maxSizeBytes, Func<HttpRequestMessage, HttpResponseMessage> respond,
        int maxRedirects = 5) =>
        new(new HttpClient(new StubHttpMessageHandler(respond)),
            ImageLimitations.WithMaxSizeBytes(maxSizeBytes),
            DownloadSettings.WithMaxRedirects(maxRedirects),
            _logger);

    private static Func<HttpRequestMessage, HttpResponseMessage> Responds(byte[] body) => _ => Ok(body);

    private static Func<HttpRequestMessage, HttpResponseMessage> Responds(HttpContent content) =>
        _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

    private static HttpResponseMessage Ok(byte[] body) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };

    private static HttpResponseMessage Redirect(string location) =>
        new(HttpStatusCode.Found) { Headers = { Location = new Uri(location, UriKind.RelativeOrAbsolute) } };

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

    /// <summary>
    /// A body no response can declare a length for. StreamContent computes Content-Length by seeking, so a
    /// stream that cannot seek is how a chunked response is reproduced here.
    /// </summary>
    private sealed class UnknownLengthStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();

            base.Dispose(disposing);
        }
    }
}
