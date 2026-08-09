using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core;

public class ImageDeleter: IImageDeleter
{
    private readonly ILogger<ImageDeleter> _logger;
    private readonly IImageMetadataRepository _metadataRepository;
    private readonly IImageBlobRepository _blobRepository;
    private readonly IImageEvents _imageEvents;

    private const string MetadataRemoving = "Removing metadata of imageId = {0}, imageGroup = {1}";
    private const string MetadataRemoved = "Metadata removed for imageId = {0}";
    private const string Removing = "Removing image and thumbnails for ImageId = {0}, ImageName = {1}, ThumbnailNames = {2}";
    private const string OriginalImageDeleted = "Original image {0} has been deleted. ";
    private const string ThumbnailsDeleted = "{0} / {1} thumbnails were deleted.";

    public ImageDeleter(ILogger<ImageDeleter> logger, IImageMetadataRepository metadataRepository,
        IImageBlobRepository blobRepository, IImageEvents imageEvents)
    {
        _logger = logger;
        _metadataRepository = metadataRepository;
        _blobRepository = blobRepository;
        _imageEvents = imageEvents;
    }

    public async Task<bool> DeleteMetadata(string imageId, string? imageGroup, CancellationToken cancellationToken)
    {
        _logger.LogDebug(MetadataRemoving, imageId, imageGroup);

        var metadata = await _metadataRepository.GetMetadata(new List<string> {imageId}, imageGroup, CancellationToken.None);
        if (metadata == null || !metadata.Any())
            return false;

        var imageMetadata = metadata[0];

        var deletedMetadata = await _metadataRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, CancellationToken.None);
        if (!deletedMetadata)
            return false;

        _logger.LogInformation(MetadataRemoved, imageId);

        await _imageEvents.ImageMetadataDeleted(imageMetadata.Id, imageMetadata.Name,
            imageMetadata.Thumbnails != null ? imageMetadata.Thumbnails.Select(x => x.Name).ToArray() : Array.Empty<string>(),
            cancellationToken);

        return true;
    }

    public async Task DeleteFiles(string imageId, string? imageName, string[] thumbnailNames, CancellationToken cancellationToken)
    {
        _logger.LogDebug(Removing, imageId, imageName, string.Join(",", thumbnailNames));

        //a null image name is rejected by the repository's own argument check, as it was before
        bool originalImageDeleted = await _blobRepository.TryDeleteImage(imageName!, ImageSizeType.Original, CancellationToken.None);
        int deletedThumbnails = 0;
        foreach (var thumbnailName in thumbnailNames)
        {
            if (await _blobRepository.TryDeleteImage(thumbnailName, ImageSizeType.Thumbnail, CancellationToken.None))
                deletedThumbnails++;
        }

        StringBuilder stringBuilder = new StringBuilder();
        if (originalImageDeleted)
            stringBuilder.AppendFormat(OriginalImageDeleted, imageId);

        stringBuilder.AppendFormat(ThumbnailsDeleted, deletedThumbnails, thumbnailNames.Length);

        _logger.LogInformation(stringBuilder.ToString());
    }
}
