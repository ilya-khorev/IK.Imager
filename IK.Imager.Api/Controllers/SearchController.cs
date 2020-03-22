using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IK.Imager.Api.Controllers
{
    /// <summary>
    /// Methods used for searching of images
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
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