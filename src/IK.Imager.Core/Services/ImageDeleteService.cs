﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Services;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Services
{
    public class ImageDeleteService: IImageDeleteService
    {
        private readonly ILogger<ImageDeleteService> _logger;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IImageBlobStorage _blobStorage;

        private const string MetadataRemoving = "Removing metadata of imageId = {0}, partitionKey = {1}";
        private const string MetadataRemoved = "Metadata removed for image {0}";
        private const string Removing = "Removing image and thumbnails for {0}";
        private const string OriginalImageDeleted = "Original image {0} has been deleted. ";
        private const string ThumbnailsDeleted = "{0} / {1} thumbnails were deleted.";
        
        public ImageDeleteService(ILogger<ImageDeleteService> logger, IImageMetadataStorage metadataStorage, IImageBlobStorage blobStorage)
        {
            _logger = logger;
            _metadataStorage = metadataStorage;
            _blobStorage = blobStorage;
        }
        
        public async Task<ImageShortInfo> DeleteOriginalImageMetadata(string imageId, string partitionKey)
        {
            _logger.LogDebug(MetadataRemoving, imageId, partitionKey);
            
            var metadata = await _metadataStorage.GetMetadata(new List<string> {imageId}, partitionKey, CancellationToken.None);
            if (metadata == null || !metadata.Any())
                return null;
            
            var deletedMetadata = await _metadataStorage.RemoveMetadata(imageId, partitionKey, CancellationToken.None);
            if (!deletedMetadata)
                return null;

            _logger.LogInformation(MetadataRemoved, imageId);
            var imageMetadata = metadata[0];

            return new ImageShortInfo
            {
                ImageId = imageMetadata.Id,
                ImageName = imageMetadata.Name,
                ThumbnailNames = imageMetadata.Thumbnails != null
                    ? imageMetadata.Thumbnails.Select(x => x.Name).ToArray()
                    : new string[0]
            };
        }

        public async Task DeleteImagesWithMetadata(ImageShortInfo imageShortInfo)
        {
            _logger.LogDebug(Removing, imageShortInfo);
            
            bool originalImageDeleted = await _blobStorage.TryDeleteImage(imageShortInfo.ImageName, ImageSizeType.Original, CancellationToken.None);
            int deletedThumbnails = 0; 
            foreach (var thumbnailName in imageShortInfo.ThumbnailNames)
            {
                if (await _blobStorage.TryDeleteImage(thumbnailName, ImageSizeType.Thumbnail, CancellationToken.None))
                    deletedThumbnails++;
            }
            
            StringBuilder stringBuilder = new StringBuilder();

            if (originalImageDeleted)
                stringBuilder.AppendFormat(OriginalImageDeleted, imageShortInfo.ImageId);

            stringBuilder.AppendFormat(ThumbnailsDeleted, imageShortInfo.ThumbnailNames.Length, deletedThumbnails);
            _logger.LogInformation(stringBuilder.ToString());
        }
    }
}