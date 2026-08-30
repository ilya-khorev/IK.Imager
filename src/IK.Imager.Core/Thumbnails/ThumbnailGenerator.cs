using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    IOptions<ImageThumbnailsSettings> imageThumbnailsSettings) : IThumbnailGenerator
{
    private readonly List<int> _configuredTargetWidths =
        imageThumbnailsSettings.Value.TargetWidth.OrderByDescending(x => x).ToList();

    private const string PngMimeType = "image/png";
    private const string PngFileExtension = "png";

    public async Task Generate(string imageId, string tenantId, CancellationToken cancellationToken)
    {
        //firstly, receiving image metadata of the given image
        var imageMetadataList = await metadataRepository.GetMetadata(new List<string> { imageId }, tenantId, cancellationToken);
        if (imageMetadataList == null || !imageMetadataList.Any())
        {
            logger.ImageNotFound(imageId);
            return;
        }

        var imageMetadata = imageMetadataList[0];
        var targetWidths = TargetWidthsFor(imageMetadata);
        imageMetadata.Thumbnails = new List<ImageThumbnail>();
        logger.ImageMetadataRead(imageMetadata.Id, imageMetadata.Width, imageMetadata.Height);
        if (imageMetadata.Width <= targetWidths[^1])
        {
            logger.ImageSmallerThanTargetWidth(imageMetadata.Id, imageMetadata.Width);
            return;
        }

        await using var originalImageStream = await blobRepository.DownloadImage(imageMetadata.BlobPath, ImageVariant.Original, cancellationToken);
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
        foreach (var targetWidth in targetWidths)
        {
            //the current image width is smaller than the target thumbnail width, so just ignoring it
            //and moving to the next target thumbnail
            if (targetWidth >= imageMetadata.Width)
                continue;

            var resizingResult = imageResizer.Resize(imageStream, imageType, targetWidth);
            logger.ImageResized(imageMetadata.Id, targetWidth, resizingResult.Size.Width, resizingResult.Size.Height, resizingResult.Size.Bytes);

            //derived from the original's path, so a thumbnail inherits its tenant, collection and unique
            //prefix - and so regenerating overwrites the previous set instead of orphaning it
            var thumbnailBlobPath = ImageBlobPath.BuildThumbnail(imageMetadata.BlobPath,
                resizingResult.Size.Width, fileExtension);

            var uploadedBlob = await blobRepository.UploadImage(thumbnailBlobPath, resizingResult.Image,
                ImageVariant.Thumbnail, mimeType, allowOverwrite: true, cancellationToken);
            imageMetadata.Thumbnails.Add(new ImageThumbnail
            {
                Id = $"{imageMetadata.Id}_{resizingResult.Size.Width}",
                BlobPath = thumbnailBlobPath,
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
        await metadataRepository.UpdateMetadata(imageMetadata, cancellationToken);
        logger.ThumbnailsGenerated(imageMetadata.Thumbnails.Count, imageId);
    }

    /// <summary>
    /// The widths the upload asked for, or the configured ones. Widest first, which is what lets each
    /// thumbnail be resized from the previous one instead of from the original.
    /// </summary>
    private List<int> TargetWidthsFor(ImageMetadata imageMetadata)
    {
        if (imageMetadata.ThumbnailTargetWidths is not { Count: > 0 } requestedWidths)
            return _configuredTargetWidths;

        logger.RequestedTargetWidths(imageMetadata.Id, requestedWidths);

        return requestedWidths.OrderByDescending(x => x).ToList();
    }
}
