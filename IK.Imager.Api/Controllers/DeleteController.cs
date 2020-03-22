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
        /// The system will remove the content after a short delay.
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete()
        {
            //delete an image -> get image metadata -> delete image metadata -> image deletion requested event (all files)
        
            //image deletion requested event: remove file + thumbnails
            
            return NoContent();
        }
    }
}