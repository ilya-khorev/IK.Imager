using System;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Contract.Upload;
using IK.Imager.Api.Tenancy;
using IK.Imager.Api.Validation;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Upload;
using IK.Imager.Core.Upload;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace IK.Imager.Api.Features.Upload;

/// <summary>
/// The two ways of getting a new image into the system - by posting its bytes, or by pointing at a url.
/// </summary>
public static class UploadEndpoints
{
    /// <summary>
    /// What a multipart upload may carry on top of the image itself: the boundary and part headers of the
    /// file and of the form fields around it. Sixteen parts of a couple of hundred bytes each, and a
    /// filename can be long - 8 KB leaves room to spare, and is nothing against a 15 MB image.
    /// </summary>
    private const int MultipartOverheadBytes = 8 * 1024;

    /// <summary>
    /// The whole of an upload-by-url body: a url, an image id of at most 128 characters, a collection of
    /// at most 30, two flags and up to 10 thumbnail widths. That leaves roughly 8 KB for the url alone -
    /// more than the request line most servers accept - so nothing legitimate comes near this, and a body
    /// that does is refused before it is buffered.
    /// </summary>
    private const int MaxUploadByUrlRequestBytes = 16 * 1024;

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder images)
    {
        //multipart form endpoints require an antiforgery token by default, which only makes sense for a
        //cookie-authenticated browser form - this is a token-less API, exactly as it was under [ApiController]
        images.MapPost("/upload", UploadImageFile)
            .WithName(nameof(UploadImageFile))
            .WithValidation<UploadImageFileRequest>()
            .DisableAntiforgery()
            .WithMetadata(new UploadSizeLimit(MaxUploadRequestBytes(images.ServiceProvider)))
            .Produces<ImageInfo>();

        images.MapPost("/upload-by-url", UploadImageByUrl)
            .WithName(nameof(UploadImageByUrl))
            .WithValidation<UploadImageByUrlRequest>()
            .WithMetadata(new UploadSizeLimit(MaxUploadByUrlRequestBytes))
            .Produces<ImageInfo>();

        return images;
    }

    /// <summary>
    /// Upload a new image using the given image byte stream.
    /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
    /// Thumbnails for the given image are available after a short delay - initially image is returned without any thumbnails.
    /// </summary>
    /// <param name="imageFileRequest"></param>
    /// <param name="imageUploader"></param>
    /// <param name="tenantContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with information about just uploaded image</returns>
    /// <response code="200">Returns the newly added image info</response>
    /// <response code="400">If the image size is greater or smaller then the system threshold values.
    /// Or if the image type is different from what the system supports.</response>
    /// <response code="409">If the given image id is already used by another image in this tenant.</response>
    internal static async Task<Ok<ImageInfo>> UploadImageFile(
        [FromForm] UploadImageFileRequest imageFileRequest,
        IImageUploader imageUploader,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var uploadImageResult = await imageUploader.Upload(
            imageFileRequest.File.OpenReadStream(), tenantContext.TenantId,
            imageFileRequest.ToOptions(), cancellationToken);

        return TypedResults.Ok(uploadImageResult.ToContract());
    }

    /// <summary>
    /// Upload a new image using the given image url.
    /// After uploading the image, the system launches the asynchronous process of thumbnails generating.
    /// Thumbnails for the given image are available after a short delay - initially image is returned to the client without any thumbnails.
    /// </summary>
    /// <param name="uploadImageByUrlRequest">Image upload request model</param>
    /// <param name="imageUploader"></param>
    /// <param name="tenantContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A model with information about just uploaded image</returns>
    /// <response code="200">Returns the newly added image info</response>
    /// <response code="400">If the given image url is empty.
    /// Or if the given image url is not well formed.
    /// Or if the image is not found by the given image url.
    /// Or if the image size is greater or smaller then the system threshold values.
    /// Or if the image type is different from what the system supports.</response>
    /// <response code="409">If the given image id is already used by another image in this tenant.</response>
    internal static async Task<Ok<ImageInfo>> UploadImageByUrl(
        UploadImageByUrlRequest uploadImageByUrlRequest,
        IImageUploader imageUploader,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var uploadImageResult = await imageUploader.UploadByUrl(
            uploadImageByUrlRequest.ImageUrl, tenantContext.TenantId,
            uploadImageByUrlRequest.ToOptions(), cancellationToken);

        return TypedResults.Ok(uploadImageResult.ToContract());
    }

    /// <summary>
    /// Bounds the multipart body, so that an image over ImageLimitations:SizeBytes.Max is refused as it
    /// arrives instead of after the form reader has spooled all of it to disk - ImageValidator only ever
    /// sees a stream that is already complete. The image upload-by-url fetches is bounded by the same
    /// setting inside ImageDownloader; what its own body needs is only MaxUploadByUrlRequestBytes.
    ///
    /// Read once at startup, because the limit is endpoint metadata. Raising SizeBytes.Max on a running
    /// host therefore moves what ImageValidator accepts but not what this refuses.
    /// </summary>
    private static long MaxUploadRequestBytes(IServiceProvider services) =>
        (long)services.GetRequiredService<IOptions<ImageLimitationsSettings>>().Value.SizeBytes.Max
        + MultipartOverheadBytes;

    /// <summary>
    /// Minimal APIs have no WithRequestSizeLimit() - the limit travels as endpoint metadata, and routing
    /// applies it to IHttpMaxRequestBodySizeFeature once the endpoint is matched. Only a real server
    /// implements that feature, so the limit does nothing under TestServer.
    /// </summary>
    private sealed record UploadSizeLimit(long? MaxRequestBodySize) : IRequestSizeLimitMetadata;

    private static ImageUploadOptions ToOptions(this UploadImageRequestBase source) =>
        new(ImageId: source.ImageId,
            Collection: source.Collection,
            IncludeCollectionInPath: source.IncludeCollectionInPath,
            AddUniquePrefix: source.AddUniquePrefix,
            ThumbnailTargetWidths: source.ThumbnailTargetWidths);

    private static ImageInfo ToContract(this ImageDetails source) =>
        new()
        {
            Id = source.Id,
            Url = source.Url.ToString(),
            Collection = source.Collection,
            Hash = source.Hash,
            DateAdded = source.DateAdded,
            Width = source.Width,
            Height = source.Height,
            Bytes = source.Bytes,
            MimeType = source.MimeType
        };
}
