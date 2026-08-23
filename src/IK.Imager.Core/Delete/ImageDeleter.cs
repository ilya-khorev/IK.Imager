using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Delete;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.Delete;

public class ImageDeleter(
    ILogger<ImageDeleter> logger,
    IImageMetadataRepository metadataRepository,
    IImageBlobRepository blobRepository,
    IImageEvents imageEvents) : IImageDeleter
{
    public async Task<bool> DeleteMetadata(string imageId, string? imageGroup, CancellationToken cancellationToken)
    {
        logger.RemovingMetadata(imageId, imageGroup);

        var metadata = await metadataRepository.GetMetadata(new List<string> { imageId }, imageGroup, cancellationToken);
        if (metadata == null || !metadata.Any())
            return false;

        var imageMetadata = metadata[0];

        var deletedMetadata = await metadataRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, cancellationToken);
        if (!deletedMetadata)
            return false;

        logger.MetadataRemoved(imageId);

        await imageEvents.ImageMetadataDeleted(imageMetadata.Id, imageMetadata.Name,
            imageMetadata.Thumbnails != null ? imageMetadata.Thumbnails.Select(x => x.Name).ToArray() : Array.Empty<string>(),
            cancellationToken);

        return true;
    }

    public async Task DeleteFiles(string imageId, string? imageName, string[] thumbnailNames, CancellationToken cancellationToken)
    {
        //the generator guards the call, not the argument, so the join stays behind an explicit check
        if (logger.IsEnabled(LogLevel.Debug))
            logger.RemovingFiles(imageId, imageName, string.Join(",", thumbnailNames));

        //a null image name is rejected by the repository's own argument check, as it was before
        bool originalImageDeleted = await blobRepository.TryDeleteImage(imageName!, ImageVariant.Original, cancellationToken);
        int deletedThumbnails = 0;
        foreach (var thumbnailName in thumbnailNames)
        {
            if (await blobRepository.TryDeleteImage(thumbnailName, ImageVariant.Thumbnail, cancellationToken))
                deletedThumbnails++;
        }

        logger.FilesDeleted(imageId, originalImageDeleted, deletedThumbnails, thumbnailNames.Length);
    }
}
