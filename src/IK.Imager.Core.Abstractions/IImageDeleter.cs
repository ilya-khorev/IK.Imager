using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions;

/// <summary>
/// Removes an image, in the two stages the system deletes in: the metadata goes first, so the image stops
/// being returned immediately, and the files follow once the event has been delivered.
/// </summary>
public interface IImageDeleter
{
    /// <summary>
    /// Removes the metadata of the given image and raises <see cref="IImageEvents.ImageMetadataDeleted"/>.
    /// Returns false when there is no such image.
    /// </summary>
    Task<bool> DeleteMetadata(string imageId, string? imageGroup, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the blob of the original image and of every thumbnail named in <paramref name="thumbnailNames"/>.
    /// </summary>
    Task DeleteFiles(string imageId, string? imageName, string[] thumbnailNames, CancellationToken cancellationToken);
}
