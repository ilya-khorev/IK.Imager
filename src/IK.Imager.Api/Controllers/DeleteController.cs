using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IK.Imager.Api.Controllers
{
    /// <summary>
    /// Used for removing an image
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class DeleteController : ControllerBase
    {
        private readonly ILogger<DeleteController> _logger;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IEventBus _eventBus;
        private readonly IOptions<TopicsConfiguration> _topicsConfiguration;

        private const string MetadataRemoved = "Metadata removed for image {0}";
        private const string ImageNotFound = "Requested image with id {0} was not found";
        
        /// <inheritdoc />
        public DeleteController(ILogger<DeleteController> logger, IImageMetadataStorage metadataStorage, IEventBus eventBus, IOptions<TopicsConfiguration> topicsConfiguration)
        {
            _logger = logger;
            _metadataStorage = metadataStorage;
            _eventBus = eventBus;
            _topicsConfiguration = topicsConfiguration;
        }
        
        /// <summary>
        /// Register the image removal by the given image id. 
        /// The system will remove the original image and thumbnail files after a short delay.
        /// However, the image itself will stop to return in search results immediately after this call.  
        /// </summary>
        /// <returns></returns>
        /// <response code="204">The image has been removed.</response>
        /// <response code="400">If the image id is not specified.</response> 
        /// <response code="404">The requested image was not found.</response> 
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(DeleteImageRequest deleteImageRequest)
        {
            var metadata = await _metadataStorage.GetMetadata(new List<string> {deleteImageRequest.ImageId},
                deleteImageRequest.PartitionKey, CancellationToken.None);
            if (metadata == null || !metadata.Any())
                return NotFound(string.Format(ImageNotFound, deleteImageRequest.ImageId));
            
            var deletedMetadata = await _metadataStorage.RemoveMetadata(deleteImageRequest.ImageId, deleteImageRequest.PartitionKey, CancellationToken.None);
            if (!deletedMetadata)
                return NotFound(string.Format(ImageNotFound, deleteImageRequest.ImageId));

            var imageMetadata = metadata[0];
            
            await _eventBus.Publish(_topicsConfiguration.Value.DeletedImagesTopicName, new ImageDeletedIntegrationEvent
            {
                ImageId = deleteImageRequest.ImageId,
                ImageName = imageMetadata.Name,
                ThumbnailNames = imageMetadata.Thumbnails != null 
                    ? imageMetadata.Thumbnails.Select(x => x.Name).ToArray() 
                    : new string[0]
            });
            
            _logger.LogInformation(MetadataRemoved, deleteImageRequest.ImageId);
            
            return NoContent();
        }
    }
}