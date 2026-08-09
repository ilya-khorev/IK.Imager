using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions;

/// <summary>
/// What the core announces once a piece of work is committed. The core calls it and knows nothing more;
/// the API layer implements it by publishing an integration event onto Azure Service Bus, which is what
/// keeps IK.Imager.Core free of any messaging dependency.
/// </summary>
public interface IImageEvents
{
    /// <summary>
    /// A new image and its metadata have been saved. Thumbnail generation follows from here, which is why
    /// thumbnails only appear in a lookup after a short delay.
    /// </summary>
    Task ImageUploaded(string imageId, string imageGroup, CancellationToken cancellationToken);

    /// <summary>
    /// The metadata of an image has been removed, so the image is already gone from lookups. Its blob and
    /// the blobs of its thumbnails are still to be deleted.
    /// </summary>
    Task ImageMetadataDeleted(string imageId, string imageName, string[] thumbnailNames, CancellationToken cancellationToken);
}
