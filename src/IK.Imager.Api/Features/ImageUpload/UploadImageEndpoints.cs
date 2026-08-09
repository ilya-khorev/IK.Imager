using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Validation;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.ImageUploading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Contract = IK.Imager.Api.Contract;
using CoreModels = IK.Imager.Core.Abstractions.Models;

#pragma warning disable 1591

namespace IK.Imager.Api.Features.ImageUpload;

/// <summary>
/// The two ways of getting a new image into the system - by posting its bytes, or by pointing at a url.
/// </summary>
public static class UploadImageEndpoints
{
    public static IEndpointRouteBuilder MapImageUploadEndpoints(this IEndpointRouteBuilder images)
    {
        //multipart form endpoints require an antiforgery token by default, which only makes sense for a
        //cookie-authenticated browser form - this is a token-less API, exactly as it was under [ApiController]
        images.MapPost("/Upload", UploadImageFile)
            .WithName(nameof(UploadImageFile))
            .WithValidation<UploadImageFileRequest>()
            .DisableAntiforgery()
            .Produces<Contract.ImageInfo>();

        images.MapPost("/UploadByUrl", UploadImageByUrl)
            .WithName(nameof(UploadImageByUrl))
            .WithValidation<Contract.UploadImageRequest>()
            .Produces<Contract.ImageInfo>();

        return images;
    }

    /// <summary>
    /// Upload a new image using the given image byte stream.
    /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
    /// Thumbnails for the given image are available after a short delay - initially image is returned without any thumbnails.
    /// </summary>
    /// <param name="imageFileRequest"></param>
    /// <param name="uploadImageCommandHandler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with information about just uploaded image</returns>
    /// <response code="200">Returns the newly added image info</response>
    /// <response code="400">If the image size is greater or smaller then the system threshold values.
    /// Or if the image type is different from what the system supports.</response>
    internal static async Task<Ok<Contract.ImageInfo>> UploadImageFile(
        [FromForm] UploadImageFileRequest imageFileRequest,
        ICommandHandler<UploadImageCommand, CoreModels.ImageInfo> uploadImageCommandHandler,
        CancellationToken cancellationToken)
    {
        var uploadImageResult = await uploadImageCommandHandler.Handle(
            new UploadImageCommand(imageFileRequest.File.OpenReadStream(), imageFileRequest.ImageGroup), cancellationToken);

        return TypedResults.Ok(uploadImageResult.ToContract());
    }

    /// <summary>
    /// Upload a new image using the given image url.
    /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
    /// Thumbnails for the given image are available after a short delay - initially image is returned to the client without any thumbnails.
    /// </summary>
    /// <param name="uploadImageRequest">Image upload request model</param>
    /// <param name="uploadImageByUrlCommandHandler"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with information about just uploaded image</returns>
    /// <response code="200">Returns the newly added image info</response>
    /// <response code="400">If the given image url is empty.
    /// Or if the given image url is not well formed.
    /// Or if the image is not found by the given image url.
    /// Or if the image size is greater or smaller then the system threshold values.
    /// Or if the image type is different from what the system supports.</response>
    internal static async Task<Ok<Contract.ImageInfo>> UploadImageByUrl(
        Contract.UploadImageRequest uploadImageRequest,
        ICommandHandler<UploadImageByUrlCommand, CoreModels.ImageInfo> uploadImageByUrlCommandHandler,
        CancellationToken cancellationToken)
    {
        var uploadImageResult = await uploadImageByUrlCommandHandler.Handle(
            new UploadImageByUrlCommand(uploadImageRequest.ImageUrl, uploadImageRequest.ImageGroup), cancellationToken);

        return TypedResults.Ok(uploadImageResult.ToContract());
    }

    //hand-written, there is no AutoMapper - and it stays inside the feature that returns the model
    private static Contract.ImageInfo ToContract(this CoreModels.ImageInfo source) =>
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
