using System;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Api.Controllers
{
    /// <summary>
    /// Set of methods which used for uploading a new image
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;

        /// <inheritdoc />
        public UploadController(ILogger<UploadController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Upload a new image using the given image byte stream.
        /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
        /// Thumbnails for the given image are available after a short delay - initially image is returned to the client without any thumbnails.
        /// </summary>
        /// <param name="file"></param>
        /// <returns>A model with short info about just uploaded image</returns>
        /// <response code="200">Returns the newly added image info</response>
        /// <response code="400">If the image size is greater or smaller then the system threshold values.
        /// Or if the image type is different from what the system supports.</response> 
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UploadImageResponse>> PostWithStream(IFormFile file)
        {
            await Task.Delay(10);

            //todo stream https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-large-files-with-streaming
            
            //todo upload image stream, image by url -> check image -> upload image file -> add image metadata -> image added event
            //image added event: get image metadata, original image file -> produce thumbnails -> update image metadata

            return Ok(new UploadImageResponse
            {
                Id = Guid.NewGuid().ToString(),
                Hash = Guid.NewGuid().ToString(),
                DateAdded = DateTimeOffset.Now,
                Bytes = 1234567,
                Height = 1000,
                Width = 1000,
                MimeType = "image/png"
            });
        }
        
        /// <summary>
        /// Upload a new image using the given image url.
        /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
        /// Thumbnails for the given image are available after a short delay - initially image is returned to the client without any thumbnails.
        /// </summary>
        /// <param name="uploadImageRequest">Image upload request model</param>
        /// <returns>A model with short info about just uploaded image</returns>
        /// <response code="200">Returns the newly added image info</response>
        /// <response code="400">If the given image url is empty.
        /// Or if the given image url is not well formatted.
        /// Or if the image is not found by the given image url.
        /// Or if the image size is greater or smaller then the system threshold values.
        /// Or if the image type is different from what the system supports.</response> 
        [HttpPost]
        [Route("WithUrl")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UploadImageResponse>> Post(UploadImageRequest uploadImageRequest)
        {
            await Task.Delay(10);
            
            //todo upload image stream, image by url -> check image -> upload image file -> add image metadata -> image added event

            return Ok(new UploadImageResponse
            {
                Id = Guid.NewGuid().ToString(),
                Hash = Guid.NewGuid().ToString(),
                DateAdded = DateTimeOffset.Now,
                Bytes = 1234567,
                Height = 1000,
                Width = 1000,
                MimeType = "image/png"
            });
        }
    }
}