using System;
using System.IO;
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
    ILogger<ImageDownloader> logger) : IImageDownloader
{
    private const int BufferSize = 81920;

    /// <summary>
    /// Returns image memory stream by a given url.
    /// Returns null when the system was not able to download the image, or when the response is larger
    /// than the configured size limit.
    /// </summary>
    public async Task<MemoryStream?> GetMemoryStream(string url, CancellationToken cancellationToken)
    {
        var maxSizeBytes = limitationSettings.CurrentValue.SizeBytes.Max;

        try
        {
            //headers first, so an oversized response is refused before its body is buffered
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength > maxSizeBytes)
            {
                logger.DownloadTooLarge(url, maxSizeBytes);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await ReadUpTo(stream, maxSizeBytes, declaredLength, url, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //the caller went away. Only the HttpClient timeout cancels without this token, and that one is
            //a failed download - so it must keep falling through to the catch below
            throw;
        }
        catch (Exception exception)
        {
            //the caller turns a null into a 400, so this is the only place the reason is ever visible
            logger.DownloadFailed(exception, url);
            return null;
        }
    }

    //Content-Length can be absent or wrong, so the copy is what enforces the limit. Anything above it is
    //rejected by ImageValidator anyway, so there is no reason to hold the rest of the response.
    private async Task<MemoryStream?> ReadUpTo(Stream source, int maxSizeBytes, long? declaredLength, string url,
        CancellationToken cancellationToken)
    {
        var capacity = declaredLength is > 0 and <= int.MaxValue ? (int)declaredLength.Value : 0;
        var imageStream = new MemoryStream(capacity);
        var buffer = new byte[BufferSize];

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            if (imageStream.Length + read > maxSizeBytes)
            {
                logger.DownloadTooLarge(url, maxSizeBytes);
                return null;
            }

            await imageStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        imageStream.Position = 0;
        return imageStream;
    }
}
