using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Mapping;
using IK.Imager.Api.Validation;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.ImageSearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Contract = IK.Imager.Api.Contract;
using CoreModels = IK.Imager.Core.Abstractions.Models;

#pragma warning disable 1591

namespace IK.Imager.Api.Features.ImageLookup;

/// <summary>
/// Looking up images that are already in the system.
/// </summary>
public static class SearchImagesEndpoints
{
    public static IEndpointRouteBuilder MapImageLookupEndpoints(this IEndpointRouteBuilder images)
    {
        images.MapPost("/Search", SearchImagesById)
            .WithName(nameof(SearchImagesById))
            .WithValidation<Contract.SearchImagesByIdRequest>()
            .Produces<Contract.ImagesSearchResult>();

        return images;
    }

    /// <summary>
    /// Search for set of images by image ids
    /// </summary>
    /// <param name="searchImagesByIdRequest">Search image request model</param>
    /// <param name="requestImagesQueryHandler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with full info about just found images. Each image is represented with the nested object.
    /// These objects are returned in the same order as they were requested.
    /// If some image is not found, this image is returned as null object.</returns>
    /// <response code="200">Returns information about images.</response>
    /// <response code="400">If the image id is not specified.
    /// Or if it is requested for more than 200 images.</response>
    internal static async Task<Ok<Contract.ImagesSearchResult>> SearchImagesById(
        Contract.SearchImagesByIdRequest searchImagesByIdRequest,
        IQueryHandler<RequestImagesQuery, CoreModels.ImagesSearchResult> requestImagesQueryHandler,
        CancellationToken cancellationToken)
    {
        var searchResult = await requestImagesQueryHandler.Handle(
            new RequestImagesQuery(searchImagesByIdRequest.ImageIds, searchImagesByIdRequest.ImageGroup), cancellationToken);

        return TypedResults.Ok(searchResult.ToContract());
    }
}
