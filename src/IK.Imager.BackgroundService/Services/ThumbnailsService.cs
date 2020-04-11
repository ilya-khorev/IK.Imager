using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.BackgroundService.Configuration;
using IK.Imager.Core.Abstractions;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageType = IK.Imager.Core.Abstractions.ImageType;
using StorageImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

namespace IK.Imager.BackgroundService.Services
{
    public class ThumbnailsService
    {
        private readonly ILogger<ThumbnailsService> _logger;
        private readonly IImageResizing _imageResizing;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly List<int> _thumbnailTargetWidth;
        
        private const string ImageNotFound = "Image metadata object with id = {0} was not found. Stopping to generate thumbnails.";
        private const string MetadataReceived = "Received original image id = {0}, width = {1}, height = {0}";
        private const string ImageDownloaded = "Downloaded original image id = {0} from storage.";
        private const string ImageResized = "Resized image id = {0} with {1}.";
        private const string ThumbnailsGenerated = "Generated {0} thumbnails for image id = {1}.";
        private const string ImageSmallerThanTargetWidth = "Image id = {0}, width = {1} is smaller than the smallest thumbnail width.";

        private const string PngMimeType = "image/png";
        
        public ThumbnailsService(ILogger<ThumbnailsService> logger, IImageResizing imageResizing,
            IImageBlobStorage blobStorage, IImageMetadataStorage metadataStorage,
            IOptions<ImageThumbnailsSettings> imageThumbnailsSettings)
        {
            _logger = logger;
            _imageResizing = imageResizing;
            _blobStorage = blobStorage;
            _metadataStorage = metadataStorage;
            _thumbnailTargetWidth = imageThumbnailsSettings.Value.TargetWidth.OrderByDescending(x => x).ToList();
        }

        public async Task<ImageMetadata> GenerateThumbnails(string imageId, string partitionKey)
        {
            var imageMetadataList = await _metadataStorage.GetMetadata(new List<string> {imageId}, partitionKey, CancellationToken.None);
            if (imageMetadataList == null || !imageMetadataList.Any())
            {
                _logger.LogInformation(ImageNotFound, imageId);
                return null;
            }

            var imageMetadata = imageMetadataList[0];
            _logger.LogDebug(MetadataReceived, imageMetadata.Id, imageMetadata.Width, imageMetadata.Height);
            if (imageMetadata.Width <= _thumbnailTargetWidth.Last())
            {
                _logger.LogInformation(ImageSmallerThanTargetWidth, imageMetadata.Id, imageMetadata.Width);
                return imageMetadata;
            }
            
            await using var originalImageStream = await _blobStorage.DownloadImage(imageMetadata.Id, ImageSizeType.Original, CancellationToken.None);
            _logger.LogDebug(ImageDownloaded, imageMetadata.Id);

            StorageImageType imageType = imageMetadata.ImageType;
            string mimeType = imageMetadata.MimeType;
            if (imageType == StorageImageType.BMP)
            {
                imageType = StorageImageType.PNG;
                mimeType = PngMimeType;
            }

            imageMetadata.Thumbnails = new List<ImageThumbnail>();
            var utcNow = DateTime.UtcNow;
            var imageStream = originalImageStream;
            foreach (var targetWidth in _thumbnailTargetWidth)
            {
                if (targetWidth >= imageMetadata.Width)
                    continue;

                var resizingResult = _imageResizing.Resize(imageStream, (ImageType) imageType, targetWidth);
                _logger.LogDebug(ImageResized, imageMetadata.Id, resizingResult.Size);
                 
                var uploadedBlob = await _blobStorage.UploadImage(resizingResult.Image, ImageSizeType.Thumbnail, mimeType, CancellationToken.None);
                imageMetadata.Thumbnails.Add(new ImageThumbnail
                {
                    Id = uploadedBlob.Id,
                    MD5Hash = uploadedBlob.MD5Hash,
                    DateAddedUtc = utcNow,
                    MimeType = imageMetadata.MimeType,
                    Height = resizingResult.Size.Height,
                    Width = resizingResult.Size.Width,
                    SizeBytes = resizingResult.Size.Bytes
                });

                imageStream = resizingResult.Image;
            }

            await imageStream.DisposeAsync();
            
            imageMetadata.Thumbnails.Reverse();
            await _metadataStorage.SetMetadata(imageMetadata, CancellationToken.None);
            _logger.LogInformation(ThumbnailsGenerated, imageMetadata.Thumbnails.Count, imageId);
            return imageMetadata;
        }
    }
}