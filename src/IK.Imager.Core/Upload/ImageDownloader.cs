using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Upload;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core.Upload;

/// <summary>
/// Downloads the bytes of an image the client only gave us a url for. A typed client - the host owns its
/// HTTP resilience, see the hook on AddImagerCore.
/// </summary>
//IOptionsMonitor rather than the IOptionsSnapshot ImageValidator takes: a typed client is registered as
//transient, and a transient must not capture a scoped dependency.
public class ImageDownloader(
    HttpClient httpClient,
    IOptionsMonitor<ImageLimitationsSettings> limitationSettings,
    IOptionsMonitor<ImageDownloadSettings> downloadSettings,
    ILogger<ImageDownloader> logger) : IImageDownloader
{
    private const int BufferSize = 81920;

    //Content-Length is whatever the remote server claims, so it sizes the buffer but is never trusted with
    //more than this. A 100 byte body claiming 15 MB would otherwise cost 15 MB of large object heap a request.
    private const int MaxPreallocatedBytes = 1024 * 1024;

    /// <summary>
    /// Returns image memory stream by a given url.
    /// Returns null when the system was not able to download the image, or when the response is larger
    /// than the configured size limit.
    /// </summary>
    public async Task<MemoryStream?> GetMemoryStream(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            logger.DownloadUrlNotAbsolute(url);
            return null;
        }

        var maxSizeBytes = limitationSettings.CurrentValue.SizeBytes.Max;

        try
        {
            using var response = await Fetch(uri, cancellationToken);
            if (response == null)
                return null;

            //the headers first, so an oversized response is refused before its body is buffered
            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength > maxSizeBytes)
            {
                logger.DownloadTooLarge(url, maxSizeBytes);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var imageStream = await ReadUpTo(stream, maxSizeBytes, declaredLength, cancellationToken);
            if (imageStream == null)
                logger.DownloadTooLarge(url, maxSizeBytes);

            return imageStream;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //the caller went away. Only the HttpClient timeout cancels without this token, and that one is
            //a failed download - so it must keep falling through to the catch below
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException)
        {
            //everything fetching a url can fail with. Anything else is a bug in this service, and a bug
            //belongs in a 500 rather than in the 400 the caller gets for a url that yielded nothing
            logger.DownloadFailed(exception, url);
            return null;
        }
    }

    /// <summary>
    /// Runs the request, following redirects a hop at a time so that every hop passes the same checks as the
    /// url the caller sent and the chain stays bounded. Returns null when the chain ended in anything other
    /// than a response to read, which is logged by then.
    /// </summary>
    private async Task<HttpResponseMessage?> Fetch(Uri uri, CancellationToken cancellationToken)
    {
        var maxRedirects = downloadSettings.CurrentValue.MaxRedirects;

        for (var redirects = 0; ; redirects++)
        {
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                logger.DownloadSchemeRefused(uri.AbsoluteUri);
                return null;
            }

            var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var redirectTo = RedirectTarget(response, uri);
            if (redirectTo == null)
            {
                if (response.IsSuccessStatusCode)
                    return response;

                //a status code is the server refusing rather than a fault, so it carries no exception
                logger.DownloadRefused(uri.AbsoluteUri, (int)response.StatusCode);
                response.Dispose();
                return null;
            }

            //nothing reads the body of a redirect, and a headers-read response holds its connection until
            //it is disposed
            response.Dispose();

            if (redirects == maxRedirects)
            {
                logger.TooManyRedirects(uri.AbsoluteUri, maxRedirects);
                return null;
            }

            uri = redirectTo;
        }
    }

    private static Uri? RedirectTarget(HttpResponseMessage response, Uri requestUri)
    {
        if (response.StatusCode is not (HttpStatusCode.MovedPermanently or HttpStatusCode.Found or
            HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect))
            return null;

        //a Location is often relative, and a redirect without a usable one is not a redirect to follow
        return response.Headers.Location != null &&
               Uri.TryCreate(requestUri, response.Headers.Location, out var target)
            ? target
            : null;
    }

    //Content-Length can be absent or wrong, so the copy is what enforces the limit. Anything above it is
    //rejected by ImageValidator anyway, so there is no reason to hold the rest of the response.
    private static async Task<MemoryStream?> ReadUpTo(Stream source, int maxSizeBytes, long? declaredLength,
        CancellationToken cancellationToken)
    {
        var imageStream = new MemoryStream((int)Math.Clamp(declaredLength ?? 0, 0, MaxPreallocatedBytes));
        var buffer = new byte[BufferSize];

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            if (imageStream.Length + read > maxSizeBytes)
                return null;

            await imageStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        imageStream.Position = 0;
        return imageStream;
    }
}
