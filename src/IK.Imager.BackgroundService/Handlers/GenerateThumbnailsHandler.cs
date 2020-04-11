using System.Threading.Tasks;
using IK.Imager.BackgroundService.Services;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.BackgroundService.Handlers
{
    public class GenerateThumbnailsHandler : IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent>
    {
        private readonly ThumbnailsService _thumbnailsService;

        public GenerateThumbnailsHandler(ThumbnailsService thumbnailsService)
        {
            _thumbnailsService = thumbnailsService;
        }

        public async Task Handle(OriginalImageUploadedIntegrationEvent iEvent)
        {
            await _thumbnailsService.GenerateThumbnails(iEvent.ImageId, iEvent.PartitionKey);
        }
    }
}