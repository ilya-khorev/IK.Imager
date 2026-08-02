using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Mapping;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Core.ImageSearch;
using IK.Imager.Core.ImageUploading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CoreModels = IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Api.Controllers;

/// <summary>
/// Set of methods which used for uploading a new image
/// </summary>
[Produces("application/json")]
[ApiController]
[Route("[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ICommandHandler<UploadImageCommand, CoreModels.ImageInfo> _uploadImageCommandHandler;
    private readonly ICommandHandler<UploadImageByUrlCommand, CoreModels.ImageInfo> _uploadImageByUrlCommandHandler;
    private readonly IQueryHandler<RequestImagesQuery, CoreModels.ImagesSearchResult> _requestImagesQueryHandler;
    private readonly ICommandHandler<DeleteImageMetadataCommand, bool> _deleteImageMetadataCommandHandler;
    private const string ImageNotFound = "Requested image with id {0} was not found";

    /// <inheritdoc />
    public ImagesController(
        ICommandHandler<UploadImageCommand, CoreModels.ImageInfo> uploadImageCommandHandler,
        ICommandHandler<UploadImageByUrlCommand, CoreModels.ImageInfo> uploadImageByUrlCommandHandler,
        IQueryHandler<RequestImagesQuery, CoreModels.ImagesSearchResult> requestImagesQueryHandler,
        ICommandHandler<DeleteImageMetadataCommand, bool> deleteImageMetadataCommandHandler)
    {
        _uploadImageCommandHandler = uploadImageCommandHandler;
        _uploadImageByUrlCommandHandler = uploadImageByUrlCommandHandler;
        _requestImagesQueryHandler = requestImagesQueryHandler;
        _deleteImageMetadataCommandHandler = deleteImageMetadataCommandHandler;
    }

    /// <summary>
    /// Upload a new image using the given image byte stream.
    /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
    /// Thumbnails for the given image are available after a short delay - initially image is returned without any thumbnails.
    /// </summary>
    /// <param name="imageFileRequest"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with information about just uploaded image</returns>
    /// <response code="200">Returns the newly added image info</response>
    /// <response code="400">If the image size is greater or smaller then the system threshold values.
    /// Or if the image type is different from what the system supports.</response> 
    [HttpPost]
    [Route("Upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImageInfo>> Post([FromForm]UploadImageFileRequest imageFileRequest, CancellationToken cancellationToken)
    {
        var uploadImageResult = await _uploadImageCommandHandler.Handle(
            new UploadImageCommand(imageFileRequest.File.OpenReadStream(), imageFileRequest.ImageGroup), cancellationToken);
        return uploadImageResult.ToContract();
    }

    /// <summary>
    /// Upload a new image using the given image url.
    /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
    /// Thumbnails for the given image are available after a short delay - initially image is returned to the client without any thumbnails.
    /// </summary>
    /// <param name="uploadImageRequest">Image upload request model</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with information about just uploaded image</returns>
    /// <response code="200">Returns the newly added image info</response>
    /// <response code="400">If the given image url is empty.
    /// Or if the given image url is not well formed.
    /// Or if the image is not found by the given image url.
    /// Or if the image size is greater or smaller then the system threshold values.
    /// Or if the image type is different from what the system supports.</response> 
    [HttpPost]
    [Route("UploadByUrl")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImageInfo>> PostByUrl(UploadImageRequest uploadImageRequest, CancellationToken cancellationToken)
    {
        var uploadImageResult = await _uploadImageByUrlCommandHandler.Handle(
            new UploadImageByUrlCommand(uploadImageRequest.ImageUrl, uploadImageRequest.ImageGroup), cancellationToken);
        return uploadImageResult.ToContract();
    }
        
    /// <summary>
    /// Search for set of images by image ids
    /// </summary>
    /// <param name="searchImagesByIdRequest">Search image request model</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with full info about just found images. Each image is represented with the nested object.
    /// These objects are returned in the same order as they were requested.
    /// If some image is not found, this image is returned as null object.</returns>
    /// <response code="200">Returns information about images.</response>
    /// <response code="400">If the image id is not specified.
    /// Or if it is requested for more than 200 images.</response> 
    [HttpPost]
    [Route("Search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImagesSearchResult>> Post(SearchImagesByIdRequest searchImagesByIdRequest, CancellationToken cancellationToken)
    {
        var searchResult = await _requestImagesQueryHandler.Handle(
            new RequestImagesQuery(searchImagesByIdRequest.ImageIds, searchImagesByIdRequest.ImageGroup), cancellationToken);
        return searchResult.ToContract();
    }
        
    /// <summary>
    /// Register the image removal by the given image id. 
    /// The system will remove the original image and thumbnail files after a short delay.
    /// However, the image itself will stop to return in search results immediately after this call.  
    /// </summary>
    /// <returns></returns>
    /// <response code="204">The image has been removed.</response>
    /// <response code="400">If the image id is not specified.</response> 
    /// <response code="404">The requested image was not found.</response> 
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(DeleteImageRequest deleteImageRequest, CancellationToken cancellationToken)
    {
        var imageDeleted = await _deleteImageMetadataCommandHandler.Handle(
            new DeleteImageMetadataCommand(deleteImageRequest.ImageId, deleteImageRequest.ImageGroup), cancellationToken);
        if (!imageDeleted)
            return NotFound(string.Format(ImageNotFound, deleteImageRequest.ImageId));

        return NoContent();
    }
}