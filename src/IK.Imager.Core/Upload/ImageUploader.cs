using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Upload;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.Upload;

public class ImageUploader(
    ILogger<ImageUploader> logger,
    IImageInspector imageInspector,
    IImageBlobRepository blobRepository,
    IImageMetadataRepository metadataRepository,
    IImageIdGenerator imageIdGenerator,
    IImageDownloader imageDownloader,
    IImageUrlBuilder imageUrlBuilder,
    IImageEvents imageEvents) : IImageUploader
{
    private const string CouldNotDownloadImage = "An image could not be downloaded by the given url.";

    public async Task<ImageDetails> UploadByUrl(string imageUrl, string tenantId, ImageUploadOptions options, CancellationToken cancellationToken)
    {
        logger.DownloadingByUrl(imageUrl);

        var imageStream = await imageDownloader.GetMemoryStream(imageUrl, cancellationToken);
        if (imageStream == null)
        {
            logger.NotDownloadedByUrl(imageUrl);

            //the request is well formed - the url simply yielded nothing - so this is the caller's error and
            //not a fault. ValidationException is what GlobalExceptionHandler turns into a 400, which is what
            //this endpoint documents for a url no image is found by.
            throw new ValidationException(CouldNotDownloadImage);
        }

        logger.DownloadedByUrl(imageUrl, imageStream.Length);

        return await Upload(imageStream, tenantId, options, cancellationToken);
    }

    public async Task<ImageDetails> Upload(Stream imageStream, string tenantId, ImageUploadOptions options, CancellationToken cancellationToken)
    {
        var (imageFormat, imageSize) = imageInspector.Inspect(imageStream);

        var imageId = options.ImageId ?? imageIdGenerator.NewImageId();

        //the id may not have existed before this point, so this is the earliest the rest of the upload - and
        //the thumbnail consumer that picks the event up later - can be tied to one image on the console
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ImageId"] = imageId,
            ["TenantId"] = tenantId
        });

        var existing = await metadataRepository.GetMetadata([imageId], tenantId, cancellationToken);
        if (existing.Count > 0)
        {
            logger.ImageIdTaken(imageId);
            throw new ImageAlreadyExistsException(tenantId, imageId);
        }

        //the extension comes from the image itself rather than from the caller, so the url cannot be
        //assembled from the id alone - it is returned instead
        var blobPath = ImageBlobPath.Build(
            tenantId,
            options.IncludeCollectionInPath ? options.Collection : null,
            options.AddUniquePrefix ? imageIdGenerator.NewUniquePrefix() : null,
            imageId,
            imageFormat.FileExtension);

        //firstly, saving the image stream to the blob storage
        BlobUploadResult uploadImageResult;
        try
        {
            uploadImageResult = await blobRepository.UploadImage(blobPath, imageStream, ImageVariant.Original,
                imageFormat.MimeType, allowOverwrite: false, cancellationToken);
        }
        catch (BlobAlreadyExistsException)
        {
            //no image owns this id, so the blob is left over from a delete that has not finished: deleting
            //drops the metadata at once and the blobs off the bus a moment later, so a just-deleted id can
            //still have its blob in place
            logger.ReplacingOrphanedBlob(blobPath);
            uploadImageResult = await blobRepository.UploadImage(blobPath, imageStream, ImageVariant.Original,
                imageFormat.MimeType, allowOverwrite: true, cancellationToken);
        }

        logger.UploadedToBlobStorage(imageId, blobPath);

        //image stream is no longer needed at this stage
        await imageStream.DisposeAsync();

        /*
         Next, saving the metadata object of this image

         If the program unexpectedly fails at this stage, there will be just a blob file, not connected to any metadata object. In this case,
         the image itself will be unavailable to the clients. And in most cases it is just fine, so an additional handling is not needed here.
        */
        try
        {
            await metadataRepository.CreateMetadata(new ImageMetadata
            {
                Id = imageId,
                TenantId = tenantId,
                Collection = options.Collection,
                BlobPath = blobPath,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MD5Hash = uploadImageResult.Hash,
                SizeBytes = imageSize.Bytes,
                MimeType = imageFormat.MimeType,
                ImageType = imageFormat.ImageType,
                FileExtension = imageFormat.FileExtension,
                //kept with the image so the thumbnail job reads them off the metadata it already fetches,
                //which is also what makes a replayed job produce the same thumbnails
                ThumbnailTargetWidths = options.ThumbnailTargetWidths?.ToList()
            }, cancellationToken);
        }
        catch (ImageAlreadyExistsException)
        {
            //the id was free at the start of this method, so another upload of it got here first. The blob
            //is deliberately left where it is: without a unique prefix both uploads share a path, so this
            //is very likely the blob the winner's metadata now points at, and deleting it would leave an
            //image that resolves to nothing. A leaked blob is the cheaper of the two.
            logger.ImageIdTakenWhileUploading(imageId, blobPath);
            throw;
        }

        logger.UploadFinished(imageId, imageSize.Bytes);

        await imageEvents.ImageUploaded(tenantId, imageId, cancellationToken);

        return new ImageDetails
        {
            Id = imageId,
            BlobPath = blobPath,
            Collection = options.Collection,
            Hash = uploadImageResult.Hash,
            DateAdded = uploadImageResult.DateAdded,
            Url = imageUrlBuilder.Build(blobPath, ImageVariant.Original),
            Bytes = imageSize.Bytes,
            Height = imageSize.Height,
            Width = imageSize.Width,
            MimeType = imageFormat.MimeType
        };
    }
}
