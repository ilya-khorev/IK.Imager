using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using Microsoft.AspNetCore.Mvc;

namespace IK.Imager.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UploadImageResponse>> Post(SearchImagesByIdRequest searchImagesByIdRequest)
        {
            //todo
            //get multiple image models by array of ids / partitionKeys
            // (if image doesn't have thumbnails for a long time -> add event)
            return Ok();
        }
    }
}