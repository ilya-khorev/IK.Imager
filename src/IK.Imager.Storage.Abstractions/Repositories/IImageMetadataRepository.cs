using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;

namespace IK.Imager.Storage.Abstractions.Repositories
{
    public interface IImageMetadataRepository
    {
        /// <summary>
        /// Inserts a new image metadata object.
        /// </summary>
        /// <param name="metadata">Image metadata</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <exception cref="ImageAlreadyExistsException">
        /// An image with the same id already exists in the same tenant.
        /// </exception>
        Task CreateMetadata(ImageMetadata metadata, CancellationToken cancellationToken);

        /// <summary>
        /// Overwrites an existing image metadata object. Used when thumbnails are written back
        /// onto an image that is already stored.
        /// </summary>
        /// <param name="metadata">Image metadata</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        Task UpdateMetadata(ImageMetadata metadata, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the metadata objects of the given images within one tenant.
        /// Ids that are not found are simply absent from the result.
        /// </summary>
        /// <param name="imageIds">Image identifiers</param>
        /// <param name="tenantId">Tenant the images belong to</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string tenantId, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the metadata object of a given image.
        /// </summary>
        /// <param name="imageId">Image identifier</param>
        /// <param name="tenantId">Tenant the image belongs to</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True, if the object was found and removed</returns>
        Task<bool> RemoveMetadata(string imageId, string tenantId, CancellationToken cancellationToken);
    }
}
