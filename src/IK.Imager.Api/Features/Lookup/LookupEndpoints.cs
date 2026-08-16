using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Contract.Lookup;
using IK.Imager.Api.Validation;
using IK.Imager.Core.Abstractions.Lookup;
using IK.Imager.Core.Abstractions.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

#pragma warning disable 1591

namespace IK.Imager.Api.Features.Lookup;

/// <summary>
/// Looking up images that are already in the system.
/// </summary>
public static class LookupEndpoints
{
    public static IEndpointRouteBuilder MapLookupEndpoints(this IEndpointRouteBuilder images)
    {
        images.MapPost("/lookup", LookupImages)
            .WithName(nameof(LookupImages))
            .WithValidation<LookupImagesRequest>()
            .Produces<LookupImagesResult>();

        return images;
    }

    /// <summary>
    /// Look up a set of images by image ids
    /// </summary>
    /// <param name="lookupImagesRequest">Image lookup request model</param>
    /// <param name="imageLookup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with full info about just found images. Each image is represented with the nested object.
    /// These objects are returned in the same order as they were requested.
    /// If some image is not found, this image is returned as null object.</returns>
    /// <response code="200">Returns information about images.</response>
    /// <response code="400">If the image id is not specified.
    /// Or if it is requested for more than 200 images.</response>
    internal static async Task<Ok<LookupImagesResult>> LookupImages(
        LookupImagesRequest lookupImagesRequest,
        IImageLookup imageLookup,
        CancellationToken cancellationToken)
    {
        var lookupResult = await imageLookup.LookupByIds(
            lookupImagesRequest.ImageIds, lookupImagesRequest.ImageGroup, cancellationToken);

        return TypedResults.Ok(lookupResult.ToContract());
    }

    //hand-written, there is no AutoMapper - and it stays inside the feature that returns the model
    private static LookupImagesResult ToContract(this ImageLookupResult source) =>
        new()
        {
            Images = source.Images.Select(ToContract).ToList()
        };

    private static ImageWithThumbnails ToContract(this ImageDetailsWithThumbnails source) =>
        new()
        {
            Id = source.Id,
            Url = source.Url.ToString(),
            Hash = source.Hash,
            DateAdded = source.DateAdded,
            Width = source.Width,
            Height = source.Height,
            Bytes = source.Bytes,
            MimeType = source.MimeType,
            Tags = source.Tags,
            Thumbnails = source.Thumbnails.Select(ToThumbnailContract).ToList()
        };

    //a thumbnail is the same shape as the model the upload feature returns, but mapping it here keeps this
    //endpoint readable on its own - a feature is not worth coupling to another one over eight assignments
    private static ImageInfo ToThumbnailContract(ImageDetails source) =>
        new()
        {
            Id = source.Id,
            Url = source.Url.ToString(),
            Hash = source.Hash,
            DateAdded = source.DateAdded,
            Width = source.Width,
            Height = source.Height,
            Bytes = source.Bytes,
            MimeType = source.MimeType
        };
}
