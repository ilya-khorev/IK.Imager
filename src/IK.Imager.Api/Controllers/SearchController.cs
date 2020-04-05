using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Api.Controllers
{
    /// <summary>
    /// Methods used for searching of images
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IEventBus _eventBus;

        public SearchController(ILogger<UploadController> logger, IImageMetadataStorage metadataStorage, IImageBlobStorage blobStorage, IEventBus eventBus)
        {
            _logger = logger;
            _metadataStorage = metadataStorage;
            _blobStorage = blobStorage;
            _eventBus = eventBus;
        }
        
        /// <summary>
        /// Search for set of images by image ids
        /// </summary>
        /// <param name="searchImagesByIdRequest">Search image request model</param>
        /// <returns>A model with full info about just found images. Each image is represented with the nested object. These objects are returned in the same order as they were requested.
        /// If some image is not found, this image is returned as null object.</returns>
        /// <response code="200">Returns information about images.</response>
        /// <response code="400">If the image id is not specified</response> 
        [HttpPost]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImageFullInfoWithThumbnails>> Post(SearchImagesByIdRequest searchImagesByIdRequest)
        {
            await Task.Delay(10);
            //todo
            //get multiple image models by array of ids / partitionKeys
            // (if image doesn't have thumbnails for a long time -> add event)
            return Ok();
        }
    }
}