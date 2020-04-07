using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Logging;

namespace IK.Imager.BackgroundService.Handlers
{
    /// <summary>
    /// Removing files of the original image and its thumbnails.
    /// Metadata object has been already removed before this event was delivered.
    /// </summary>
    public class RemoveImageFilesHandler: IIntegrationEventHandler<ImageDeletedIntegrationEvent>
    {
        private readonly ILogger<RemoveImageFilesHandler> _logger;
        private readonly IImageBlobStorage _blobStorage;

        private const string OriginalImageDeleted = "Original image {0} has been deleted. ";
        private const string ThumbnailsDeleted = "{0} / {1} thumbnails were deleted.";
        
        public RemoveImageFilesHandler(ILogger<RemoveImageFilesHandler> logger, IImageBlobStorage blobStorage)
        {
            _logger = logger;
            _blobStorage = blobStorage;
        }
        
        public async Task Handle(ImageDeletedIntegrationEvent iEvent)
        {
            bool originalImageDeleted = await _blobStorage.TryDeleteImage(iEvent.ImageId, ImageType.Original, CancellationToken.None);
            int deletedThumbnails = 0;
            foreach (var thumbnailId in iEvent.ThumbnailsIds)
            {
                if (await _blobStorage.TryDeleteImage(thumbnailId, ImageType.Thumbnail, CancellationToken.None))
                    deletedThumbnails++;
            }
            
            StringBuilder stringBuilder = new StringBuilder();

            if (originalImageDeleted)
                stringBuilder.AppendFormat(OriginalImageDeleted, iEvent.ImageId);

            stringBuilder.AppendFormat(ThumbnailsDeleted, iEvent.ThumbnailsIds.Length, deletedThumbnails);
            
            _logger.LogInformation(stringBuilder.ToString());
        }
    }
}