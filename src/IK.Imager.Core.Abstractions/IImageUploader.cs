using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions;

/// <summary>
/// Gets a new image into the system: validates it, stores the file and its metadata, and raises
/// <see cref="IImageEvents.ImageUploaded"/> so thumbnails are generated afterwards.
/// </summary>
public interface IImageUploader
{
    /// <summary>
    /// Uploads the image carried by <paramref name="imageStream"/>. The stream is consumed and disposed.
    /// </summary>
    Task<ImageInfo> Upload(Stream imageStream, string imageGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the image at <paramref name="imageUrl"/> and uploads it.
    /// </summary>
    Task<ImageInfo> UploadByUrl(string imageUrl, string imageGroup, CancellationToken cancellationToken);
}
