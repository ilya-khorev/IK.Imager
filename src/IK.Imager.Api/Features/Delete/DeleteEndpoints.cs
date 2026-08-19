using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Contract.Delete;
using IK.Imager.Api.Validation;
using IK.Imager.Core.Abstractions.Delete;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

#pragma warning disable 1591

namespace IK.Imager.Api.Features.Delete;

/// <summary>
/// Removing an image from the system.
/// </summary>
public static class DeleteEndpoints
{
    private const string ImageNotFound = "Requested image with id {0} was not found";

    public static IEndpointRouteBuilder MapDeleteEndpoints(this IEndpointRouteBuilder images)
    {
        images.MapDelete("/{imageId}", DeleteImage)
            .WithName(nameof(DeleteImage))
            .WithValidation<DeleteImageRequest>();

        return images;
    }

    /// <summary>
    /// Register the image removal by the given image id.
    /// The system will remove the original image and thumbnail files after a short delay.
    /// However, the image itself will stop to return in lookup results immediately after this call.
    /// </summary>
    /// <param name="deleteImageRequest">Image removal request model</param>
    /// <param name="imageDeleter"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <response code="204">The image has been removed.</response>
    /// <response code="400">If the image id is not specified.</response>
    /// <response code="404">The requested image was not found.</response>
    //the id is a route value and the group a query string, so there is no body to bind - which is what
    //keeps this off the one shape minimal APIs refuse to infer a body for
    internal static async Task<Results<NoContent, NotFound<string>>> DeleteImage(
        [AsParameters] DeleteImageRequest deleteImageRequest,
        IImageDeleter imageDeleter,
        CancellationToken cancellationToken)
    {
        var imageDeleted = await imageDeleter.DeleteMetadata(
            deleteImageRequest.ImageId, deleteImageRequest.ImageGroup, cancellationToken);

        if (!imageDeleted)
            return TypedResults.NotFound(string.Format(ImageNotFound, deleteImageRequest.ImageId));

        return TypedResults.NoContent();
    }
}
