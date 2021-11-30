using System.Threading.Tasks;
using IK.Imager.Api.Commands;
using IK.Imager.Api.Contract;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IK.Imager.Api.Controllers
{
    /// <summary>
    /// Methods used for searching of images
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <inheritdoc /> 
        public SearchController(IMediator mediator)
        {
            _mediator = mediator;
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ImagesSearchResult>> Post(SearchImagesByIdRequest searchImagesByIdRequest)
        {
            var uploadImageResult = await _mediator.Send(new RequestImagesCommand(searchImagesByIdRequest.ImageIds, searchImagesByIdRequest.ImageGroup));
            return uploadImageResult;
        }
    }
}