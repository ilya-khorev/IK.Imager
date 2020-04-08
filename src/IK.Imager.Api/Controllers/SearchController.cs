using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Models;
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
        private readonly ILogger<SearchController> _logger;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IEventBus _eventBus;

        private const string TooManyImagesRequested = "Maximum allowed images to be requested within one API call is 100.";
        private const string FoundImages = "Found {0} image(s) for requested {1} image id(s)";
        
        /// <inheritdoc />
        public SearchController(ILogger<SearchController> logger, IImageMetadataStorage metadataStorage, IImageBlobStorage blobStorage, IEventBus eventBus)
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
        /// <response code="400">If the image id is not specified.
        /// Or if it is requested for more than 100 images.</response> 
        [HttpPost]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImagesSearchResult>> Post(SearchImagesByIdRequest searchImagesByIdRequest)
        {
            const int maxAllowedImages = 100;
            if (searchImagesByIdRequest.ImageIds.Length > maxAllowedImages)
            {
                _logger.LogWarning(TooManyImagesRequested);
                return BadRequest(TooManyImagesRequested);
            }
            
            var imagesMetadata = await _metadataStorage.GetMetadata(searchImagesByIdRequest.ImageIds, searchImagesByIdRequest.PartitionKey, CancellationToken.None);

            ImagesSearchResult result = new ImagesSearchResult
            {
                Images = new List<ImageFullInfoWithThumbnails>(imagesMetadata.Count)
            };

            foreach (var imageMetadata in imagesMetadata)
            {
                var model = new ImageFullInfoWithThumbnails
                {
                    Id = imageMetadata.Id,
                    Bytes = imageMetadata.SizeBytes,
                    Hash = imageMetadata.MD5Hash,
                    Height = imageMetadata.Height,
                    Width = imageMetadata.Width,
                    Tags = imageMetadata.Tags ?? new Dictionary<string, string>(),
                    Url = _blobStorage.GetImageUri(imageMetadata.Id, ImageSizeType.Original).ToString(),
                    DateAdded = imageMetadata.DateAddedUtc,
                    MimeType = imageMetadata.MimeType,
                    Thumbnails = new List<ImageInfo>()
                };

                //todo if an image was added a long time ago and there are not any thumbnails, it's worth sending a new event to generate them
                
                if (imageMetadata.Thumbnails != null)
                    foreach (var thumbnail in imageMetadata.Thumbnails)
                    {
                        model.Thumbnails.Add(new ImageInfo
                        {
                            Id = thumbnail.Id,
                            Bytes = thumbnail.SizeBytes,
                            Hash = thumbnail.MD5Hash,
                            Height = thumbnail.Height,
                            Width = thumbnail.Width,
                            DateAdded = thumbnail.DateAddedUtc,
                            MimeType = thumbnail.MimeType,
                            Url = _blobStorage.GetImageUri(thumbnail.Id, ImageSizeType.Thumbnail).ToString()
                        });
                    }
                    
                result.Images.Add(model);
            }
            
            _logger.LogInformation(FoundImages, result.Images.Count, searchImagesByIdRequest.ImageIds.Length);
            
            return Ok(result);
        }
    }
}