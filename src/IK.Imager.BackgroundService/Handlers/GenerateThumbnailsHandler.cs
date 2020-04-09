using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.BackgroundService.Configuration;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageType = IK.Imager.Core.Abstractions.ImageType;
using StorageImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

namespace IK.Imager.BackgroundService.Handlers
{
    public class GenerateThumbnailsHandler: IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent>
    {
        private readonly ILogger<GenerateThumbnailsHandler> _logger;
        private readonly IImageResizing _imageResizing;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IOptions<ImageThumbnailsSettings> _imageThumbnailsSettings;

        private const string ImageNotFound = "Image metadata object with id = {0} was not found. Stopping to generate thumbnails.";
        private const string ImageDownloaded = "Downloaded original image id = {0} from storage.";
        private const string ImageResized = "Resized image id = {0} with {1}.";
        private const string ThumbnailsGenerated = "Generated {0} thumbnails for image id = {1}.";

        public GenerateThumbnailsHandler(ILogger<GenerateThumbnailsHandler> logger, IImageResizing imageResizing, IImageBlobStorage blobStorage, IImageMetadataStorage metadataStorage, IOptions<ImageThumbnailsSettings> imageThumbnailsSettings)
        {
            _logger = logger;
            _imageResizing = imageResizing;
            _blobStorage = blobStorage;
            _metadataStorage = metadataStorage;
            _imageThumbnailsSettings = imageThumbnailsSettings;
        }
        
        public async Task Handle(OriginalImageUploadedIntegrationEvent iEvent)
        { 
            var imageMetadataList = await _metadataStorage.GetMetadata(new List<string> {iEvent.ImageId}, iEvent.PartitionKey, CancellationToken.None);
            if (imageMetadataList == null || !imageMetadataList.Any())
            {
                _logger.LogInformation(ImageNotFound, iEvent.ImageId);
                return;
            }

            var imageMetadata = imageMetadataList[0];
            var originalImageStream = await _blobStorage.DownloadImage(imageMetadata.Id, ImageSizeType.Original, CancellationToken.None);
            _logger.LogDebug(ImageDownloaded, imageMetadata.Id);
            
            StorageImageType imageType = imageMetadata.ImageType;
            string mimeType = imageMetadata.MimeType;
            if (imageType == StorageImageType.BMP)
            {
                imageType = StorageImageType.PNG;
                mimeType = "image/png";
            }

            imageMetadata.Thumbnails = new List<ImageThumbnail>();
            foreach (var targetWidth in _imageThumbnailsSettings.Value.TargetWidth.OrderBy(x => x))
            {
                var resizingResult = _imageResizing.Resize(originalImageStream, (ImageType) imageType, targetWidth);
                _logger.LogDebug(ImageResized, imageMetadata.Id, resizingResult.Size);
                var uploadImageResult = await _blobStorage.UploadImage(resizingResult.Image, ImageSizeType.Thumbnail, mimeType, CancellationToken.None);
                imageMetadata.Thumbnails.Add(new ImageThumbnail
                {
                    Id = uploadImageResult.Id,
                    MD5Hash = uploadImageResult.MD5Hash,
                    DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                    MimeType = imageMetadata.MimeType,
                    Height = resizingResult.Size.Height,
                    Width = resizingResult.Size.Width,
                    SizeBytes = resizingResult.Size.Bytes
                });
            }

            await _metadataStorage.SetMetadata(imageMetadata, CancellationToken.None);
            _logger.LogInformation(ThumbnailsGenerated, imageMetadata.Thumbnails.Count, iEvent.ImageId);
        }
    }
}