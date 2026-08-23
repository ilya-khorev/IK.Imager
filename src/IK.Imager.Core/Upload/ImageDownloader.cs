using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Upload;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

/// <summary>
/// Downloads the bytes of an image the client only gave us a url for. A typed client - the host owns its
/// HTTP resilience, see the hook on AddImagerCore.
/// </summary>
public class ImageDownloader(HttpClient httpClient, ILogger<ImageDownloader> logger) : IImageDownloader
{
    /// <summary>
    /// Returns image memory stream by a given url.
    /// Returns null when the system was not able to download the image.
    /// </summary>
    public async Task<MemoryStream?> GetMemoryStream(string url, CancellationToken cancellationToken)
    {
        MemoryStream imageStream = new MemoryStream();
        try
        {
            await using var stream = await httpClient.GetStreamAsync(url, cancellationToken);
            await stream.CopyToAsync(imageStream, cancellationToken);
            imageStream.Position = 0;
        }
        catch (Exception exception)
        {
            //the caller turns a null into a 400, so this is the only place the reason is ever visible
            logger.DownloadFailed(exception, url);
            return null;
        }

        return imageStream;
    }
}
