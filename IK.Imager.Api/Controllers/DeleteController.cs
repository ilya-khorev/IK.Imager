using IK.Imager.Api.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IK.Imager.Api.Controllers
{
    /// <summary>
    /// Used for removing an image
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class DeleteController : ControllerBase
    {
        /// <summary>
        /// Register the file and metadata removal for the given image id. 
        /// The system will remove the content asynchronously after a short delay.
        /// </summary>
        /// <returns></returns>
        /// <response code="204">The image has been removed</response>
        /// <response code="400">If the image id is not specified</response> 
        /// <response code="404">The requested image was not found</response> 
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(DeleteImageRequest deleteImageRequest)
        {
            //delete an image -> get image metadata -> delete image metadata -> image deletion requested event (all files)
        
            //image deletion requested event: remove file + thumbnails
            
            return NoContent();
        }
    }
}