using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions.Thumbnails;

/// <summary>
/// Generates the thumbnails of an already uploaded image and attaches them to its metadata.
/// </summary>
public interface IThumbnailGenerator
{
    /// <summary>
    /// Generates a thumbnail per configured target width for the given image. Does nothing when the image
    /// is not found, or when it is narrower than the smallest target width.
    /// </summary>
    Task Generate(string imageId, string imageGroup, CancellationToken cancellationToken);
}
