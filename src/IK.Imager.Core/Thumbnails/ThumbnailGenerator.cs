using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageType = IK.Imager.Core.Abstractions.Models.ImageType;
using StorageImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

#pragma warning disable 1591

namespace IK.Imager.Core.Thumbnails;

public class ThumbnailGenerator(
    ILogger<ThumbnailGenerator> logger,
    IImageResizing imageResizing,
    IImageBlobRepository blobRepository,
    IImageMetadataRepository metadataRepository,
    IImageIdentifierProvider imageIdentifierProvider,
    IOptions<ImageThumbnailsSettings> imageThumbnailsSettings) : IThumbnailGenerator
{
    private readonly List<int> _thumbnailTargetWidths =
        imageThumbnailsSettings.Value.TargetWidth.OrderByDescending(x => x).ToList();

    private const string ImageNotFound =
        "Image metadata object with id = {0} was not found. Stopping to generate thumbnails.";

    private const string MetadataReceived = "Received original image id = {0}, width = {1}, height = {0}";
    private const string ImageDownloaded = "Downloaded original image id = {0} from storage.";
    private const string ImageResized = "Resized image id = {0} with {1}.";
    private const string ThumbnailsGenerated = "Generated {0} thumbnails for image id = {1}.";

    private const string ImageSmallerThanTargetWidth =
        "Image id = {0}, width = {1} is smaller than the smallest thumbnail width.";

    private const string PngMimeType = "image/png";
    private const string PngFileExtension = ".png";

    public async Task Generate(string imageId, string imageGroup, CancellationToken cancellationToken)
    {
        //firstly, receiving image metadata of the given image
        var imageMetadataList = await metadataRepository.GetMetadata(new List<string> { imageId }, imageGroup, cancellationToken);
        if (imageMetadataList == null || !imageMetadataList.Any())
        {
            logger.LogInformation(ImageNotFound, imageId);
            return;
        }

        var imageMetadata = imageMetadataList[0];
        imageMetadata.Thumbnails = new List<ImageThumbnail>();
        logger.LogDebug(MetadataReceived, imageMetadata.Id, imageMetadata.Width, imageMetadata.Height);
        if (imageMetadata.Width <= _thumbnailTargetWidths.Last())
        {
            logger.LogInformation(ImageSmallerThanTargetWidth, imageMetadata.Id, imageMetadata.Width);
            return;
        }

        await using var originalImageStream = await blobRepository.DownloadImage(imageMetadata.Name, ImageSizeType.Original, cancellationToken);
        logger.LogDebug(ImageDownloaded, imageMetadata.Id);

        StorageImageType imageType = imageMetadata.ImageType;
        string mimeType = imageMetadata.MimeType;
        string fileExtension = imageMetadata.FileExtension;
        if (imageType == StorageImageType.BMP)
        {
            imageType = StorageImageType.PNG;
            mimeType = PngMimeType;
            fileExtension = PngFileExtension;
        }

        //a blob is expected to exist for metadata that exists; a missing one still fails loudly below, as before
        var imageStream = originalImageStream!;
        foreach (var targetWidth in _thumbnailTargetWidths)
        {
            //the current image width is smaller than the target thumbnail width, so just ignoring it
            //and moving to the next target thumbnail
            if (targetWidth >= imageMetadata.Width)
                continue;

            var resizingResult = imageResizing.Resize(imageStream, (ImageType)imageType, targetWidth);
            logger.LogDebug(ImageResized, imageMetadata.Id, resizingResult.Size);

            var thumbnailImageId = imageIdentifierProvider.GenerateUniqueId();
            var thumbnailImageName = imageIdentifierProvider.GetImageFileName(thumbnailImageId, fileExtension);

            var uploadedBlob = await blobRepository.UploadImage(thumbnailImageName, resizingResult.Image,
                ImageSizeType.Thumbnail, mimeType, cancellationToken);
            imageMetadata.Thumbnails.Add(new ImageThumbnail
            {
                Id = thumbnailImageId,
                Name = thumbnailImageName,
                MD5Hash = uploadedBlob.Hash,
                DateAddedUtc = uploadedBlob.DateAdded.DateTime,
                MimeType = mimeType,
                Height = resizingResult.Size.Height,
                Width = resizingResult.Size.Width,
                SizeBytes = resizingResult.Size.Bytes
            });

            //keeping reference to the resized image, so that the further thumbnail is generated faster
            imageStream = resizingResult.Image;
        }

        await imageStream.DisposeAsync();

        imageMetadata.Thumbnails.Reverse(); //smaller thumbnails come first
        await metadataRepository.SetMetadata(imageMetadata, cancellationToken);
        logger.LogInformation(ThumbnailsGenerated, imageMetadata.Thumbnails.Count, imageId);
    }
}
