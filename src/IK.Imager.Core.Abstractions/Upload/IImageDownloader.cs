using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions.Upload;

/// <summary>
/// Fetches the bytes of an image the caller only gave a url for. Behind an interface like every other
/// dependency of <see cref="IImageUploader"/> - the implementation is a typed HttpClient, and the host
/// owns its resilience policy.
/// </summary>
public interface IImageDownloader
{
    /// <summary>
    /// Returns the image as a memory stream, or null when nothing could be downloaded from the url, or
    /// when the response is larger than the configured size limit.
    /// </summary>
    Task<MemoryStream?> GetMemoryStream(string url, CancellationToken cancellationToken);
}
