using System.Threading.Tasks;
using AutoMapper;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions.ImageDeleting;
using IK.Imager.Core.Abstractions.ImageSearch;
using IK.Imager.Core.Abstractions.ImageUploading;
using IK.Imager.Core.ImageDeleting;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IK.Imager.Api.Controllers
{
    /// <summary>
    /// Set of methods which used for uploading a new image
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private const string ImageNotFound = "Requested image with id {0} was not found";

        /// <inheritdoc />
        public ImagesController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>
        /// Upload a new image using the given image byte stream.
        /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
        /// Thumbnails for the given image are available after a short delay - initially image is returned without any thumbnails.
        /// </summary>
        /// <param name="imageFileRequest"></param>
        /// <returns>A model with information about just uploaded image</returns>
        /// <response code="200">Returns the newly added image info</response>
        /// <response code="400">If the image size is greater or smaller then the system threshold values.
        /// Or if the image type is different from what the system supports.</response> 
        [HttpPost]
        [Route("Upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ImageInfo>> Post([FromForm]UploadImageFileRequest imageFileRequest)
        {
            var uploadImageResult = await _mediator.Send(new UploadImageCommand(imageFileRequest.File.OpenReadStream(), imageFileRequest.ImageGroup));
            return _mapper.Map<ImageInfo>(uploadImageResult);
        }

        /// <summary>
        /// Upload a new image using the given image url.
        /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
        /// Thumbnails for the given image are available after a short delay - initially image is returned to the client without any thumbnails.
        /// </summary>
        /// <param name="uploadImageRequest">Image upload request model</param>
        /// <returns>A model with information about just uploaded image</returns>
        /// <response code="200">Returns the newly added image info</response>
        /// <response code="400">If the given image url is empty.
        /// Or if the given image url is not well formed.
        /// Or if the image is not found by the given image url.
        /// Or if the image size is greater or smaller then the system threshold values.
        /// Or if the image type is different from what the system supports.</response> 
        [HttpPost]
        [Route("UploadByUrl")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImageInfo>> PostByUrl(UploadImageRequest uploadImageRequest)
        {
            var uploadImageResult = await _mediator.Send(new UploadImageByUrlCommand(uploadImageRequest.ImageUrl, uploadImageRequest.ImageGroup));
            return _mapper.Map<ImageInfo>(uploadImageResult);
        }
        
        /// <summary>
        /// Search for set of images by image ids
        /// </summary>
        /// <param name="searchImagesByIdRequest">Search image request model</param>
        /// <returns>A model with full info about just found images. Each image is represented with the nested object.
        /// These objects are returned in the same order as they were requested.
        /// If some image is not found, this image is returned as null object.</returns>
        /// <response code="200">Returns information about images.</response>
        /// <response code="400">If the image id is not specified.
        /// Or if it is requested for more than 200 images.</response> 
        [HttpPost]
        [Route("Search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImagesSearchResult>> Post(SearchImagesByIdRequest searchImagesByIdRequest)
        {
            var uploadImageResult = await _mediator.Send(new RequestImagesQuery(searchImagesByIdRequest.ImageIds, searchImagesByIdRequest.ImageGroup));
            return _mapper.Map<ImagesSearchResult>(uploadImageResult);
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
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(DeleteImageRequest deleteImageRequest)
        {
            var imageDeleted = await _mediator.Send(new DeleteImageMetadataCommand(deleteImageRequest.ImageId, deleteImageRequest.ImageGroup));
            if (!imageDeleted)
                return NotFound(string.Format(ImageNotFound, deleteImageRequest.ImageId));

            return NoContent();
        }
    }
}