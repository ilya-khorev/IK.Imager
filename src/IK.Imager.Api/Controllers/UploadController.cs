﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Configuration;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Services;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IImageMetadataReader _metadataReader;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IEventBus _eventBus;
        private readonly IOptions<ImageLimitationSettings> _limitationSettings;
        private readonly ImageDownloadClient _imageDownloadClient;

        private const string UnsupportedFormat = "Unsupported image format. Please use one of the following formats: {0}";
        private const string IncorrectSize = "Image size must be between {0} and {1} bytes";
        private const string IncorrectDimensions = "Image width must be between {0} and {1} px. Image height must be between {2} and {3} px.";
        private const string IncorrectUrlFormat = "Image url is not well formed. It must be absolute url path.";
        private const string CouldNotDownloadImage = "Couldn't download image by url {0}";
        
        /// <inheritdoc />
        public UploadController(ILogger<UploadController> logger, IImageMetadataReader metadataReader, IImageBlobStorage blobStorage, IImageMetadataStorage metadataStorage, 
            IEventBus eventBus, IOptions<ImageLimitationSettings> limitationSettings, ImageDownloadClient imageDownloadClient)
        {
            _logger = logger;
            _metadataReader = metadataReader;
            _blobStorage = blobStorage;
            _metadataStorage = metadataStorage;
            _eventBus = eventBus;
            _limitationSettings = limitationSettings;
            _imageDownloadClient = imageDownloadClient;
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
        //todo probably worth uploading using stream https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-large-files-with-streaming
        public async Task<ActionResult<ImageInfo>> PostWithStream(IFormFile file)
        {
            string partitionKey = "123"; //todo
            return await UploadImage(file.OpenReadStream(), partitionKey);
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
        /// Or if the given image url is not well formed.
        /// Or if the image is not found by the given image url.
        /// Or if the image size is greater or smaller then the system threshold values.
        /// Or if the image type is different from what the system supports.</response> 
        [HttpPost]
        [Route("WithUrl")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImageInfo>> Post(UploadImageRequest uploadImageRequest)
        {
            if (!Uri.IsWellFormedUriString(uploadImageRequest.ImageUrl, UriKind.Absolute))
                return BadRequest(IncorrectUrlFormat);

            var imageStream = await _imageDownloadClient.GetMemoryStream(uploadImageRequest.ImageUrl);
            if (imageStream == null)
                return BadRequestAndLog(string.Format(CouldNotDownloadImage, uploadImageRequest.ImageUrl));

            return await UploadImage(imageStream, uploadImageRequest.PartitionKey);
        }
        
        private async Task<ActionResult<ImageInfo>> UploadImage(Stream imageStream, string partitionKey)
        {
            var imageFormat = _metadataReader.DetectFormat(imageStream); 
            var limits = _limitationSettings.Value;
            
            if (imageFormat == null || !limits.Types.Contains(imageFormat.ImageType.ToString(), StringComparer.InvariantCultureIgnoreCase))
                return BadRequestAndLog(string.Format(UnsupportedFormat, string.Join(",", limits.Types)));

            //todo if it's bmp, worth converting to png
            
            var imageSize = _metadataReader.ReadSize(imageStream);
            var sizeLimits = limits.SizeBytes;
            if (imageSize.Bytes > sizeLimits.Max || imageSize.Bytes < sizeLimits.Min)
                return BadRequestAndLog(string.Format(IncorrectSize, sizeLimits.Min, sizeLimits.Max));

            var widthLimits = limits.Width;
            var heightLimits = limits.Height;
            if (imageSize.Width > widthLimits.Max || imageSize.Width < widthLimits.Min || imageSize.Height > heightLimits.Max || imageSize.Height < heightLimits.Min)
                return BadRequestAndLog(string.Format(IncorrectDimensions, widthLimits.Min, widthLimits.Max, heightLimits.Min, heightLimits.Max));

            //Firstly, saving the image stream to the blob storage
            var uploadImageResult = await _blobStorage.UploadImage(imageStream, ImageType.Original, imageFormat.MimeType, CancellationToken.None);

            //image stream is no longer needed
            await imageStream.DisposeAsync();
            
            //Next, saving the metadata object of this image
            //When the program unexpectedly fails at this stage, there will be just a blob file not connected to any metadata object
            //and therefore the image will be unavailable to the clients. In most cases it is just fine.
            await _metadataStorage.SetMetadata(new ImageMetadata
            {
                Id = uploadImageResult.Id,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MD5Hash = uploadImageResult.MD5Hash,
                SizeBytes = imageSize.Bytes,
                MimeType = imageFormat.MimeType,
                PartitionKey = partitionKey 
            }, CancellationToken.None);
            
            //Once the image file and metadata object are saved, there is time to send a new message to the event bus topic
            //If the program fails at this stage, this message is not sent and therefore thumbnails are not generated for the image. 
            //Such cases are handled when requesting an image metadata object later by resending this event again.
            await _eventBus.Publish("UploadedImages", new OriginalImageUploadedIntegrationEvent
            {
                ImageId = uploadImageResult.Id,
                PartitionKey = partitionKey 
            });
            
            return Ok(new ImageInfo
            {
                Id = uploadImageResult.Id,
                Hash = uploadImageResult.MD5Hash,
                DateAdded = uploadImageResult.DateAdded,
                Url = uploadImageResult.Url.ToString(),
                Bytes = imageSize.Bytes,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MimeType = imageFormat.MimeType
            });
        }

        private BadRequestObjectResult BadRequestAndLog(string message)
        {
            _logger.LogWarning(message);
            return BadRequest(message);
        }
    }
}