using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Services;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.IntegrationEvents;

namespace IK.Imager.BackgroundService.Handlers
{
    public class GenerateThumbnailsHandler : IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent>
    {
        private readonly IImageThumbnailService _thumbnailsService;

        public GenerateThumbnailsHandler(IImageThumbnailService thumbnailsService)
        {
            _thumbnailsService = thumbnailsService;
        }

        public async Task Handle(OriginalImageUploadedIntegrationEvent iEvent)
        {
            await _thumbnailsService.GenerateThumbnails(iEvent.ImageId, iEvent.PartitionKey);
        }
    }
}