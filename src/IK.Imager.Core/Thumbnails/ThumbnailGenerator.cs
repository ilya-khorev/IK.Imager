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

#pragma warning disable 1591

namespace IK.Imager.Core.Thumbnails;

public class ThumbnailGenerator(
    ILogger<ThumbnailGenerator> logger,
    IImageResizer imageResizer,
    IImageBlobRepository blobRepository,
    IImageMetadataRepository metadataRepository,
    IImageNameGenerator imageNameGenerator,
    IOptions<ImageThumbnailsSettings> imageThumbnailsSettings) : IThumbnailGenerator
{
    private readonly List<int> _thumbnailTargetWidths =
        imageThumbnailsSettings.Value.TargetWidth.OrderByDescending(x => x).ToList();

    private const string PngMimeType = "image/png";
    private const string PngFileExtension = ".png";

    public async Task Generate(string imageId, string imageGroup, CancellationToken cancellationToken)
    {
        //firstly, receiving image metadata of the given image
        var imageMetadataList = await metadataRepository.GetMetadata(new List<string> { imageId }, imageGroup, cancellationToken);
        if (imageMetadataList == null || !imageMetadataList.Any())
        {
            logger.ImageNotFound(imageId, imageGroup);
            return;
        }

        var imageMetadata = imageMetadataList[0];
        imageMetadata.Thumbnails = new List<ImageThumbnail>();
        logger.ImageMetadataRead(imageMetadata.Id, imageMetadata.Width, imageMetadata.Height);
        if (imageMetadata.Width <= _thumbnailTargetWidths.Last())
        {
            logger.ImageSmallerThanTargetWidth(imageMetadata.Id, imageMetadata.Width);
            return;
        }

        await using var originalImageStream = await blobRepository.DownloadImage(imageMetadata.Name, ImageVariant.Original, cancellationToken);
        logger.OriginalImageDownloaded(imageMetadata.Id);

        ImageType imageType = imageMetadata.ImageType;
        string mimeType = imageMetadata.MimeType;
        string fileExtension = imageMetadata.FileExtension;
        if (imageType == ImageType.BMP)
        {
            imageType = ImageType.PNG;
            mimeType = PngMimeType;
            fileExtension = PngFileExtension;
        }

        //a blob is expected to exist for metadata that exists; a missing one now comes back as null from the
        //repository and still fails loudly on the first resize rather than generating empty thumbnails
        var imageStream = originalImageStream!;
        foreach (var targetWidth in _thumbnailTargetWidths)
        {
            //the current image width is smaller than the target thumbnail width, so just ignoring it
            //and moving to the next target thumbnail
            if (targetWidth >= imageMetadata.Width)
                continue;

            var resizingResult = imageResizer.Resize(imageStream, imageType, targetWidth);
            logger.ImageResized(imageMetadata.Id, targetWidth, resizingResult.Size.Width, resizingResult.Size.Height, resizingResult.Size.Bytes);

            var thumbnailImageId = imageNameGenerator.NewImageId();
            var thumbnailImageName = imageNameGenerator.ToFileName(thumbnailImageId, fileExtension);

            var uploadedBlob = await blobRepository.UploadImage(thumbnailImageName, resizingResult.Image,
                ImageVariant.Thumbnail, mimeType, cancellationToken);
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
        logger.ThumbnailsGenerated(imageMetadata.Thumbnails.Count, imageId);
    }
}
