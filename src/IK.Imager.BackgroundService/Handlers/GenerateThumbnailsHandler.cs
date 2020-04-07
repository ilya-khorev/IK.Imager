using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Logging;

namespace IK.Imager.BackgroundService.Handlers
{
    public class GenerateThumbnailsHandler: IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent>
    {
        private readonly ILogger<GenerateThumbnailsHandler> _logger;
        private readonly IImageResizing _imageResizing;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;

        public GenerateThumbnailsHandler(ILogger<GenerateThumbnailsHandler> logger, IImageResizing imageResizing, IImageBlobStorage blobStorage, IImageMetadataStorage metadataStorage)
        {
            _logger = logger;
            _imageResizing = imageResizing;
            _blobStorage = blobStorage;
            _metadataStorage = metadataStorage;
        }
        
        public async Task Handle(OriginalImageUploadedIntegrationEvent iEvent)
        { 
            var imageMetadataList = await _metadataStorage.GetMetadata(new List<string> {iEvent.ImageId}, iEvent.PartitionKey, CancellationToken.None);
            if (imageMetadataList == null || !imageMetadataList.Any())
            {
                _logger.LogInformation("Image metadata object with id = {0} was not found. Stopping to generate thumbnails.", iEvent.ImageId);
                return;
            }

            var imageMetadata = imageMetadataList[0];
            
            //todo generating thumnails
            //todo if it's bmp, worth converting to png
            
            //todo saving image files
            
            //todo updating metdata
            
            await _metadataStorage.SetMetadata(imageMetadata, CancellationToken.None);
        }
    }
}