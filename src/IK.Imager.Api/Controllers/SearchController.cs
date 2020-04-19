using System.Threading.Tasks;
using AutoMapper;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions.Services;
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
        private readonly IImageSearchService _imageSearchService;
        private readonly IMapper _mapper;

        private const string TooManyImagesRequested = "Maximum allowed images to be requested within one API call is 100.";
        
        /// <inheritdoc /> 
        public SearchController(ILogger<SearchController> logger, IImageSearchService imageSearchService, IMapper mapper)
        {
            _logger = logger;
            _imageSearchService = imageSearchService;
            _mapper = mapper;
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

            var searchResult = await _imageSearchService.Search(searchImagesByIdRequest.ImageIds,
                searchImagesByIdRequest.PartitionKey);

            return Ok(_mapper.Map<ImagesSearchResult>(searchResult));
        }
    }
}