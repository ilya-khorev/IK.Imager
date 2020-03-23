using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

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
        private readonly IImageMetadataReader _imageMetadataReader;
        private readonly IImageStorage _imageStorage;
        private readonly IImageMetadataStorage _imageMetadataStorage;
        private readonly IEventBus _eventBus;

        /// <inheritdoc />
        public UploadController(ILogger<UploadController> logger, IImageMetadataReader imageMetadataReader, IImageStorage imageStorage, IImageMetadataStorage imageMetadataStorage, IEventBus eventBus)
        {
            _logger = logger;
            _imageMetadataReader = imageMetadataReader;
            _imageStorage = imageStorage;
            _imageMetadataStorage = imageMetadataStorage;
            _eventBus = eventBus;
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
        //todo probably consider uploading using stream https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-large-files-with-streaming
        public async Task<ActionResult<ImageInfo>> PostWithStream(IFormFile file)
        {
            return await UploadImage(file.OpenReadStream());
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
        public async Task<ActionResult<ImageInfo>> Post(UploadImageRequest uploadImageRequest)
        {
            MemoryStream stream = new MemoryStream(); //todo

            return await UploadImage(stream);
        }

        private async Task<ActionResult<ImageInfo>> UploadImage(Stream imageStream)
        {
            var imageFormat = _imageMetadataReader.DetectFormat(imageStream);
            var imageSize = _imageMetadataReader.ReadSize(imageStream);
            //todo check if size and format are in a range of provided in config

            var uploadImageResult = await _imageStorage.UploadImage(imageStream, ImageType.Original, imageFormat.MimeType, CancellationToken.None);
            await _imageMetadataStorage.SetMetadata(new ImageMetadata
            {
                Id = uploadImageResult.Id,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MD5Hash = uploadImageResult.MD5Hash,
                SizeBytes = imageSize.Bytes,
                MimeType = imageFormat.MimeType,
                PartitionKey = "" //todo pass partitionKey
            }, CancellationToken.None);
            
            //todo add additional config values and fill event model
            await _eventBus.Publish("NewImages", new OriginalImageAddedIntegrationEvent());
            
            return Ok(new ImageInfo
            {
                Id = uploadImageResult.Id,
                Hash = uploadImageResult.MD5Hash,
                DateAdded = uploadImageResult.DateAdded,
                Bytes = imageSize.Bytes,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MimeType = imageFormat.MimeType
            });
        }
    }
}