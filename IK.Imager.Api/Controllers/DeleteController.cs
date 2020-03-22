using Microsoft.AspNetCore.Mvc;

namespace IK.Imager.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeleteController : ControllerBase
    {
        [HttpDelete]
        public IActionResult Delete()
        {
            //delete an image -> get image metadata -> delete image metadata -> image deletion requested event (all files)
        
            //image deletion requested event: remove file + thumbnails
            
            return NoContent();
        }
    }
}