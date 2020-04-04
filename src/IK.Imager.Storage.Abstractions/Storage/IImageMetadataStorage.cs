using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;

namespace IK.Imager.Storage.Abstractions.Storage
{
    public interface IImageMetadataStorage
    {
        /// <summary>
        /// Insert or update an image metadata object
        /// </summary>
        /// <param name="metadata">Image metadata</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task SetMetadata(ImageMetadata metadata, CancellationToken cancellationToken);
        
        /// <summary>
        /// Get a list of metadata object for a given set of image ids
        /// PartitionKey is required for this operation, therefore it's more optimized in terms of performance
        /// </summary>
        /// <param name="imageIds">Image identifiers</param>
        /// <param name="partitionKey">Partition key where to search the objects</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string partitionKey, CancellationToken cancellationToken);
        
        /// <summary>
        /// Get a list of metadata object for a given set of image ids
        /// </summary>
        /// <param name="imageIds">Image identifiers</param>
        /// <param name="cancellationToken">Cancellation token to stop operation</param>
        /// <returns></returns>
        Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, CancellationToken cancellationToken);
        
        /// <summary>
        /// Removes a metadata object
        /// </summary>
        /// <param name="imageId">Image identifier</param>
        /// <param name="partitionKey">Partition key</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> RemoveMetadata(string imageId, string partitionKey, CancellationToken cancellationToken);
    }
}