using System;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Services;
using IK.Imager.Core.Abstractions.ImagesCrud;
using IK.Imager.Core.Settings;
using IK.Imager.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        private readonly IImageUploadService _imageUploadService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ImageDownloadClient _imageDownloadClient;
        private readonly IOptionsSnapshot<ImageLimitationSettings> _imageLimitationSettings;
        private readonly IMapper _mapper;

        private const string IncorrectUrlFormat = "Image url is not well formed. It must be absolute url path.";
        private const string CouldNotDownloadImage = "Couldn't download image by url {0}.";
        private const string DownloadingByUrl = "Downloading image by url {0}.";
        private const string DownloadedByUrl = "Downloaded image by url {0}.";

        /// <inheritdoc />
        public UploadController(ILogger<UploadController> logger, IImageUploadService imageUploadService, 
            IPublishEndpoint publishEndpoint, ImageDownloadClient imageDownloadClient, 
            IOptionsSnapshot<ImageLimitationSettings> imageLimitationSettings, IMapper mapper)
        {
            _logger = logger;
            _imageUploadService = imageUploadService;
            _publishEndpoint = publishEndpoint;
            _imageDownloadClient = imageDownloadClient;
            _imageLimitationSettings = imageLimitationSettings;
            _mapper = mapper;
        }

        /// <summary>
        /// Upload a new image using the given image byte stream.
        /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
        /// Thumbnails for the given image are available after a short delay - initially image is returned to the client without any thumbnails.
        /// </summary>
        /// <param name="imageFileRequest"></param>
        /// <returns>A model with information about just uploaded image</returns>
        /// <response code="200">Returns the newly added image info</response>
        /// <response code="400">If the image size is greater or smaller then the system threshold values.
        /// Or if the image type is different from what the system supports.</response> 
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Consumes("multipart/form-data")]
        //todo probably worth uploading using stream https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-large-files-with-streaming
        public async Task<ActionResult<ImageInfo>> Post([FromForm]UploadImageFileRequest imageFileRequest)
        {
            return await UploadImage(imageFileRequest.File.OpenReadStream(), imageFileRequest.ImageGroup, imageFileRequest.LimitationSettings);
        }
        
        //todo add image restrictions as a part of the request model

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
        [Route("ByUrl")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImageInfo>> PostByUrl(UploadImageRequest uploadImageRequest)
        {
            if (!Uri.IsWellFormedUriString(uploadImageRequest.ImageUrl, UriKind.Absolute))
                return BadRequest(IncorrectUrlFormat);

            _logger.LogDebug(DownloadingByUrl, uploadImageRequest.ImageUrl);

            var imageStream = await _imageDownloadClient.GetMemoryStream(uploadImageRequest.ImageUrl);
            if (imageStream == null)
                return BadRequestAndLog(string.Format(CouldNotDownloadImage, uploadImageRequest.ImageUrl));

            _logger.LogDebug(DownloadedByUrl, uploadImageRequest.ImageUrl);
            
            return await UploadImage(imageStream, uploadImageRequest.ImageGroup, uploadImageRequest.LimitationSettings);
        }
        
        private BadRequestObjectResult BadRequestAndLog(string message)
        {
            _logger.LogWarning(message);
            return BadRequest(message);
        }

        private async Task<ActionResult<ImageInfo>> UploadImage(Stream imageStream, string imageGroup, ImageLimitationSettingsRequest imageLimitationSettings)
        {
            OverrideImageLimitationSettings(imageLimitationSettings);
            
            var uploadImageResult = await _imageUploadService.UploadImage(imageStream, imageGroup);
            
            //Once the image file and metadata object are saved, there is time to send a new message to the event bus topic
            //If the program fails at this stage, this message is not sent and therefore thumbnails are not generated for the image. 
            //Such cases are handled when requesting an image metadata object later by resending this event again.
            await _publishEndpoint.Publish(new OriginalImageUploadedIntegrationEvent
            {
                ImageId = uploadImageResult.Id,
                ImageGroup = imageGroup
            });
            
            return Ok(_mapper.Map<ImageInfo>(uploadImageResult));
        }

        private void OverrideImageLimitationSettings(ImageLimitationSettingsRequest imageLimitationSettingsRequest)
        {
            if (imageLimitationSettingsRequest == null)
                return;

            if (imageLimitationSettingsRequest.Height != null)
            {
                _imageLimitationSettings.Value.Height = new Core.Settings.Range<int>
                {
                    Max = imageLimitationSettingsRequest.Height.Max,
                    Min = imageLimitationSettingsRequest.Height.Min
                };
            }
            
            if (imageLimitationSettingsRequest.Width != null)
            {
                _imageLimitationSettings.Value.Width = new Core.Settings.Range<int>
                {
                    Max = imageLimitationSettingsRequest.Width.Max,
                    Min = imageLimitationSettingsRequest.Width.Min
                };
            }
            
            if (imageLimitationSettingsRequest.SizeBytes != null)
            {
                _imageLimitationSettings.Value.SizeBytes = new Core.Settings.Range<int>
                {
                    Max = imageLimitationSettingsRequest.SizeBytes.Max,
                    Min = imageLimitationSettingsRequest.SizeBytes.Min
                };
            }
            
            if (imageLimitationSettingsRequest.AspectRatio != null)
            {
                _imageLimitationSettings.Value.AspectRatio = new Core.Settings.Range<double>
                {
                    Max = imageLimitationSettingsRequest.AspectRatio.Max,
                    Min = imageLimitationSettingsRequest.AspectRatio.Min
                };
            }
            
            if (imageLimitationSettingsRequest.Types != null)
            {
                _imageLimitationSettings.Value.Types = imageLimitationSettingsRequest.Types;
            }
        }
    }
}