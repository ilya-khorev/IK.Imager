using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    IImageNameGenerator imageNameGenerator,
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

        var imageId = options.ImageId ?? imageNameGenerator.NewImageId();

        //the id may not have existed before this point, so this is the earliest the rest of the upload - and
        //the thumbnail consumer that picks the event up later - can be tied to one image on the console
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ImageId"] = imageId,
            ["TenantId"] = tenantId
        });

        //the extension comes from the image itself rather than from the caller, so the url cannot be
        //assembled from the id alone - it is returned instead
        var blobPath = imageNameGenerator.BuildBlobPath(
            tenantId,
            options.IncludeCollectionInPath ? options.Collection : null,
            options.AddUniquePrefix ? imageNameGenerator.NewUniquePrefix() : null,
            imageId,
            imageFormat.FileExtension);

        //Firstly, saving the image stream to the blob storage
        BlobUploadResult uploadImageResult;
        try
        {
            uploadImageResult = await blobRepository.UploadImage(blobPath, imageStream, ImageVariant.Original,
                imageFormat.MimeType, allowOverwrite: false, cancellationToken);
        }
        catch (BlobAlreadyExistsException ex)
        {
            //without a unique prefix the path is a function of the id, so a taken id is caught here first.
            //A blob on its own does not mean the id is taken though: deleting an image drops its metadata at
            //once and its blobs off the bus a moment later, so re-uploading a just-deleted id lands here with
            //nothing owning the blob. Metadata is what makes an image exist, so that is what decides.
            var existing = await metadataRepository.GetMetadata([imageId], tenantId, cancellationToken);
            if (existing.Count > 0)
            {
                logger.ImageIdTaken(imageId);
                throw new ImageAlreadyExistsException(tenantId, imageId, ex);
            }

            logger.ReplacingOrphanedBlob(blobPath);
            uploadImageResult = await blobRepository.UploadImage(blobPath, imageStream, ImageVariant.Original,
                imageFormat.MimeType, allowOverwrite: true, cancellationToken);
        }

        logger.UploadedToBlobStorage(imageId, blobPath);

        //Image stream is no longer needed at this stage
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
                FileExtension = imageFormat.FileExtension
            }, cancellationToken);
        }
        catch (ImageAlreadyExistsException)
        {
            //with a unique prefix the blob path is new every time, so the clash only surfaces here - and the
            //blob just written is one nothing will ever point at
            await blobRepository.TryDeleteImage(blobPath, ImageVariant.Original, cancellationToken);
            logger.ImageIdTaken(imageId);
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
