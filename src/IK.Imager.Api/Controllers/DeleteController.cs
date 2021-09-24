﻿using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions.ImagesCrud;
using IK.Imager.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IOptions<TopicsConfiguration> _topicsConfiguration;
        private readonly IImageDeleteService _imageDeleteService;
 
        private const string ImageNotFound = "Requested image with id {0} was not found";
        
        /// <inheritdoc />
        public DeleteController(IPublishEndpoint publishEndpoint, IOptions<TopicsConfiguration> topicsConfiguration, IImageDeleteService imageDeleteService)
        {
            _publishEndpoint = publishEndpoint;
            _topicsConfiguration = topicsConfiguration;
            _imageDeleteService = imageDeleteService;
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
            var imageDeleteResult =
                await _imageDeleteService.DeleteImageMetadata(deleteImageRequest.ImageId,
                    deleteImageRequest.ImageGroup);

            if (imageDeleteResult == null)
                return NotFound(string.Format(ImageNotFound, deleteImageRequest.ImageId));
           
            await _publishEndpoint.Publish(new ImageDeletedIntegrationEvent
            {
                ImageId = imageDeleteResult.ImageId,
                ImageName = imageDeleteResult.ImageName,
                ThumbnailNames = imageDeleteResult.ThumbnailNames
            });

            return NoContent();
        }
        
        //todo remove all files by the image group
    }
}