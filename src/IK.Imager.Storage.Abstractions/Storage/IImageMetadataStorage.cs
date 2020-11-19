using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;

namespace IK.Imager.Storage.Abstractions.Storage
{
    public interface IImageMetadataStorage
    {
        /// <summary>
        /// Inserts or updates an image metadata object
        /// </summary>
        /// <param name="metadata">Image metadata</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task SetMetadata(ImageMetadata metadata, CancellationToken cancellationToken);
        
        /// <summary>
        /// Returns a list of metadata object for a given set of image ids and a given image group
        /// </summary>
        /// <param name="imageIds">Image identifiers</param>
        /// <param name="imageGroup">Image group to which the requested images belong</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string imageGroup, CancellationToken cancellationToken);
        
        /// <summary>
        /// Returns a list of metadata object for a given set of image ids
        /// </summary>
        /// <param name="imageIds">Image identifiers</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, CancellationToken cancellationToken);
        
        /// <summary>
        /// Removes a metadata object for a given image id an image group
        /// </summary>
        /// <param name="imageId">Image identifier</param>
        /// <param name="imageGroup">Image group of the requested image</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if the object was found and removed</returns>
        Task<bool> RemoveMetadata(string imageId, string imageGroup, CancellationToken cancellationToken);
    }
}