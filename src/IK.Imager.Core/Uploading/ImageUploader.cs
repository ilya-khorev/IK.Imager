using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Uploading;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.Uploading;

public class ImageUploader(
    ILogger<ImageUploader> logger,
    IImageMetadataReader metadataReader,
    IImageBlobRepository blobRepository,
    IImageMetadataRepository metadataRepository,
    IImageValidator imageValidator,
    IImageIdentifierProvider imageIdentifierProvider,
    ImageDownloadClient imageDownloadClient,
    IImageEvents imageEvents) : IImageUploader
{
    private const string CheckingImage = "Starting to check the image.";
    private const string UploadedToBlobStorage = "Uploaded the image to the blob storage, imageId={0}.";
    private const string UploadingFinished = "Image with id={0} and its metadata have been saved.";
    private const string DownloadingByUrl = "Downloading an image by url {0}.";
    private const string DownloadedByUrl = "Downloaded an image by url {0}.";

    public async Task<ImageInfo> UploadByUrl(string imageUrl, string imageGroup, CancellationToken cancellationToken)
    {
        logger.LogDebug(DownloadingByUrl, imageUrl);

        var imageStream = await imageDownloadClient.GetMemoryStream(imageUrl, cancellationToken);
        if (imageStream == null)
        {
            //todo handle
        }

        logger.LogDebug(DownloadedByUrl, imageUrl);

        //the null case above is still unhandled (see the todo); the ! preserves the existing behaviour of failing downstream
        return await Upload(imageStream!, imageGroup, cancellationToken);
    }

    public async Task<ImageInfo> Upload(Stream imageStream, string imageGroup, CancellationToken cancellationToken)
    {
        var (imageFormat, imageSize) = Inspect(imageStream);

        //Firstly, saving the image stream to the blob storage
        string imageId = imageIdentifierProvider.GenerateUniqueId();
        string imageName = imageIdentifierProvider.GetImageFileName(imageId, imageFormat.FileExtension);

        //todo original: id_with_height.jpg
        //todo thumbnail: widthxheight/originalid_width_height.jpg

        //todo check if such name already exist (it's unlikely, but worth checking)

        var uploadImageResult = await blobRepository.UploadImage(imageName, imageStream, ImageSizeType.Original, imageFormat.MimeType, cancellationToken);
        logger.LogDebug(UploadedToBlobStorage, imageId);

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
            ImageType = (Storage.Abstractions.Models.ImageType)imageFormat.ImageType,
            FileExtension = imageFormat.FileExtension,
            ImageGroup = imageGroup
        }, cancellationToken);

        logger.LogInformation(string.Format(UploadingFinished, imageId));

        await imageEvents.ImageUploaded(imageId, imageGroup, cancellationToken);

        return new ImageInfo
        {
            Id = imageId,
            Name = imageName,
            Hash = uploadImageResult.Hash,
            DateAdded = uploadImageResult.DateAdded,
            Url = uploadImageResult.Url,
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
        logger.LogDebug(CheckingImage);

        var imageFormat = metadataReader.DetectFormat(imageStream);
        //CheckFormat already reports a null format as invalid; the explicit null test is what tells the compiler so
        if (!imageValidator.CheckFormat(imageFormat).IsValid || imageFormat == null)
            throw new ValidationException(); //todo return error model instead of exception

        logger.LogDebug(imageFormat.ToString());

        //ReadSize only returns null for a stream ImageSharp cannot identify, which DetectFormat has just ruled out
        var imageSize = metadataReader.ReadSize(imageStream);
        if (imageSize == null || !imageValidator.CheckSize(imageSize).IsValid)
            throw new ValidationException(); //todo return error model instead of exception

        logger.LogDebug(imageSize.ToString());

        return (imageFormat, imageSize);
    }
}
