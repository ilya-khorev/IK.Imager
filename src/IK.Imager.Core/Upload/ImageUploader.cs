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

    public async Task<ImageDetails> UploadByUrl(string imageUrl, string imageGroup, CancellationToken cancellationToken)
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

        return await Upload(imageStream, imageGroup, cancellationToken);
    }

    public async Task<ImageDetails> Upload(Stream imageStream, string imageGroup, CancellationToken cancellationToken)
    {
        var (imageFormat, imageSize) = imageInspector.Inspect(imageStream);

        //Firstly, saving the image stream to the blob storage
        string imageId = imageNameGenerator.NewImageId();
        string imageName = imageNameGenerator.ToFileName(imageId, imageFormat.FileExtension);

        //the id does not exist before this point, so this is the earliest the rest of the upload - and the
        //thumbnail consumer that picks the event up later - can be tied to one image on the console
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ImageId"] = imageId,
            ["ImageGroup"] = imageGroup
        });

        //todo original: id_with_height.jpg
        //todo thumbnail: widthxheight/originalid_width_height.jpg

        //todo check if such name already exist (it's unlikely, but worth checking)

        var uploadImageResult = await blobRepository.UploadImage(imageName, imageStream, ImageVariant.Original, imageFormat.MimeType, cancellationToken);
        logger.UploadedToBlobStorage(imageId, imageName);

        //Image stream is no longer needed at this stage
        imageStream.Dispose();

        /*
         Next, saving the metadata object of this image

         If the program unexpectedly fails at this stage, there will be just a blob file, not connected to any metadata object. In this case,
         the image itself will be unavailable to the clients. And in most cases it is just fine, so an additional handling is not needed here.
        */
        await metadataRepository.SetMetadata(new ImageMetadata
        {
            Id = imageId,
            Name = imageName,
            DateAddedUtc = uploadImageResult.DateAdded.DateTime,
            Height = imageSize.Height,
            Width = imageSize.Width,
            MD5Hash = uploadImageResult.Hash,
            SizeBytes = imageSize.Bytes,
            MimeType = imageFormat.MimeType,
            ImageType = imageFormat.ImageType,
            FileExtension = imageFormat.FileExtension,
            ImageGroup = imageGroup
        }, cancellationToken);

        logger.UploadFinished(imageId, imageGroup, imageSize.Bytes);

        await imageEvents.ImageUploaded(imageId, imageGroup, cancellationToken);

        return new ImageDetails
        {
            Id = imageId,
            Name = imageName,
            Hash = uploadImageResult.Hash,
            DateAdded = uploadImageResult.DateAdded,
            Url = imageUrlBuilder.Build(imageName, ImageVariant.Original),
            Bytes = imageSize.Bytes,
            Height = imageSize.Height,
            Width = imageSize.Width,
            MimeType = imageFormat.MimeType
        };
    }
}
