using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Validation;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.ImageDeleting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Contract = IK.Imager.Api.Contract;

#pragma warning disable 1591

namespace IK.Imager.Api.Features.ImageDeleting;

/// <summary>
/// Removing an image from the system.
/// </summary>
public static class DeleteImageEndpoints
{
    private const string ImageNotFound = "Requested image with id {0} was not found";

    public static IEndpointRouteBuilder MapImageDeletingEndpoints(this IEndpointRouteBuilder images)
    {
        images.MapDelete("/", DeleteImage)
            .WithName(nameof(DeleteImage))
            .WithValidation<Contract.DeleteImageRequest>();

        return images;
    }

    /// <summary>
    /// Register the image removal by the given image id.
    /// The system will remove the original image and thumbnail files after a short delay.
    /// However, the image itself will stop to return in search results immediately after this call.
    /// </summary>
    /// <param name="deleteImageRequest">Image removal request model</param>
    /// <param name="deleteImageMetadataCommandHandler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <response code="204">The image has been removed.</response>
    /// <response code="400">If the image id is not specified.</response>
    /// <response code="404">The requested image was not found.</response>
    //DELETE is one of the methods minimal APIs refuse to infer a body for, so the source is explicit here
    internal static async Task<Results<NoContent, NotFound<string>>> DeleteImage(
        [FromBody] Contract.DeleteImageRequest deleteImageRequest,
        ICommandHandler<DeleteImageMetadataCommand, bool> deleteImageMetadataCommandHandler,
        CancellationToken cancellationToken)
    {
        var imageDeleted = await deleteImageMetadataCommandHandler.Handle(
            new DeleteImageMetadataCommand(deleteImageRequest.ImageId, deleteImageRequest.ImageGroup), cancellationToken);

        if (!imageDeleted)
            return TypedResults.NotFound(string.Format(ImageNotFound, deleteImageRequest.ImageId));

        return TypedResults.NoContent();
    }
}
