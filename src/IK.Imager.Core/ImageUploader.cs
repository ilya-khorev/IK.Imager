using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core;

public class ImageUploader: IImageUploader
{
    private readonly ILogger<ImageUploader> _logger;
    private readonly IImageMetadataReader _metadataReader;
    private readonly IImageBlobRepository _blobRepository;
    private readonly IImageMetadataRepository _metadataRepository;
    private readonly IImageValidator _imageValidator;
    private readonly IImageIdentifierProvider _imageIdentifierProvider;
    private readonly ImageDownloadClient _imageDownloadClient;
    private readonly IImageEvents _imageEvents;

    private const string CheckingImage = "Starting to check the image.";
    private const string UploadedToBlobStorage = "Uploaded the image to the blob storage, imageId={0}.";
    private const string UploadingFinished = "Image with id={0} and its metadata have been saved.";
    private const string DownloadingByUrl = "Downloading an image by url {0}.";
    private const string DownloadedByUrl = "Downloaded an image by url {0}.";

    public ImageUploader(ILogger<ImageUploader> logger, IImageMetadataReader metadataReader, IImageBlobRepository blobRepository,
        IImageMetadataRepository metadataRepository, IImageValidator imageValidator, IImageIdentifierProvider imageIdentifierProvider,
        ImageDownloadClient imageDownloadClient, IImageEvents imageEvents)
    {
        _logger = logger;
        _metadataReader = metadataReader;
        _blobRepository = blobRepository;
        _metadataRepository = metadataRepository;
        _imageValidator = imageValidator;
        _imageIdentifierProvider = imageIdentifierProvider;
        _imageDownloadClient = imageDownloadClient;
        _imageEvents = imageEvents;
    }

    //todo too many dependencies, split into several classes

    public async Task<ImageInfo> UploadByUrl(string imageUrl, string imageGroup, CancellationToken cancellationToken)
    {
        _logger.LogDebug(DownloadingByUrl, imageUrl);

        var imageStream = await _imageDownloadClient.GetMemoryStream(imageUrl);
        if (imageStream == null)
        {
            //todo handle
        }

        _logger.LogDebug(DownloadedByUrl, imageUrl);

        //the null case above is still unhandled (see the todo); the ! preserves the existing behaviour of failing downstream
        return await Upload(imageStream!, imageGroup, cancellationToken);
    }

    public async Task<ImageInfo> Upload(Stream imageStream, string imageGroup, CancellationToken cancellationToken)
    {
        _logger.LogDebug(CheckingImage);
        var imageFormat = _metadataReader.DetectFormat(imageStream);
        var validationResult = _imageValidator.CheckFormat(imageFormat);
        //CheckFormat already reports a null format as invalid; the explicit null test is what tells the compiler so
        if (!validationResult.IsValid || imageFormat == null)
            throw new ValidationException(); //todo return error model instead of exception

        _logger.LogDebug(imageFormat.ToString());

        //ReadSize only returns null for a stream ImageSharp cannot identify, which DetectFormat has just ruled out
        var imageSize = _metadataReader.ReadSize(imageStream);
        if (imageSize == null)
            throw new ValidationException(); //todo return error model instead of exception

        validationResult = _imageValidator.CheckSize(imageSize);
        if (!validationResult.IsValid)
            throw new ValidationException(); //todo return error model instead of exception

        _logger.LogDebug(imageSize.ToString());

        //Firstly, saving the image stream to the blob storage
        string imageId = _imageIdentifierProvider.GenerateUniqueId();
        string imageName = _imageIdentifierProvider.GetImageFileName(imageId, imageFormat.FileExtension);

        //todo original: id_with_height.jpg
        //todo thumbnail: widthxheight/originalid_width_height.jpg

        //todo check if such name already exist (it's unlikely, but worth checking)

        var uploadImageResult = await _blobRepository.UploadImage(imageName, imageStream, ImageSizeType.Original, imageFormat.MimeType, CancellationToken.None);
        _logger.LogDebug(UploadedToBlobStorage, imageId);

        //Image stream is no longer needed at this stage
        imageStream.Dispose();

        /*
         Next, saving the metadata object of this image

         If the program unexpectedly fails at this stage, there will be just a blob file, not connected to any metadata object. In this case,
         the image itself will be unavailable to the clients. And in most cases it is just fine, so an additional handling is not needed here.
        */
        await _metadataRepository.SetMetadata(new ImageMetadata
        {
            Id = imageId,
            Name = imageName,
            DateAddedUtc = uploadImageResult.DateAdded.DateTime,
            Height = imageSize.Height,
            Width = imageSize.Width,
            MD5Hash = uploadImageResult.Hash,
            SizeBytes = imageSize.Bytes,
            MimeType = imageFormat.MimeType,
            ImageType = (Storage.Abstractions.Models.ImageType) imageFormat.ImageType,
            FileExtension = imageFormat.FileExtension,
            ImageGroup = imageGroup
        }, CancellationToken.None);

        _logger.LogInformation(string.Format(UploadingFinished, imageId));

        await _imageEvents.ImageUploaded(imageId, imageGroup, cancellationToken);

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
}
