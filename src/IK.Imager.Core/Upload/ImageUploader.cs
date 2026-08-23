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
    IImageValidator imageValidator,
    IImageNameGenerator imageNameGenerator,
    IImageDownloader imageDownloader,
    IImageUrlBuilder imageUrlBuilder,
    IImageEvents imageEvents) : IImageUploader
{
    private const string CouldNotDownloadImage = "An image could not be downloaded by the given url.";
    private const string CouldNotReadImageSize = "The size of the image could not be read.";

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
        var (imageFormat, imageSize) = Inspect(imageStream);

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

    /// <summary>
    /// Reads the format and the size off the stream and checks both against the configured limits.
    /// </summary>
    private (ImageFormat Format, ImageSize Size) Inspect(Stream imageStream)
    {
        logger.CheckingImage();

        var imageFormat = imageInspector.DetectFormat(imageStream);
        var formatResult = imageValidator.CheckFormat(imageFormat);
        //CheckFormat already reports a null format as invalid; the explicit null test is what tells the compiler so
        if (!formatResult.IsValid || imageFormat == null)
            throw Reject(formatResult);

        logger.ImageFormatDetected(imageFormat.MimeType, imageFormat.ImageType, imageFormat.FileExtension);

        //ReadSize only returns null for a stream ImageSharp cannot identify, which DetectFormat has just ruled out
        var imageSize = imageInspector.ReadSize(imageStream);
        if (imageSize == null)
            throw new ValidationException(CouldNotReadImageSize);

        var sizeResult = imageValidator.CheckSize(imageSize);
        if (!sizeResult.IsValid)
            throw Reject(sizeResult);

        logger.ImageSizeRead(imageSize.Width, imageSize.Height, imageSize.Bytes, imageSize.AspectRatio);

        return (imageFormat, imageSize);
    }

    //the keys are a bounded set, so they stay queryable as a log property; the messages are for the 400 body
    private ValidationException Reject(ImageValidationResult validationResult)
    {
        logger.ImageRejected(string.Join(", ", validationResult.ValidationErrors.Select(x => x.Key)));

        return new ValidationException(string.Join(" ", validationResult.ValidationErrors.Select(x => x.ErrorMessage)));
    }
}
