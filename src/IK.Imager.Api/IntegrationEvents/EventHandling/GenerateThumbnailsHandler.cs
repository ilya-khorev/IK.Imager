using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Thumbnails;
using MassTransit;

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling
{
    public class GenerateThumbnailsHandler : IConsumer<OriginalImageUploadedIntegrationEvent>
    {
        private readonly IImageThumbnailService _thumbnailsService;

        public GenerateThumbnailsHandler(IImageThumbnailService thumbnailsService)
        {
            _thumbnailsService = thumbnailsService;
        }
        
        public async Task Consume(ConsumeContext<OriginalImageUploadedIntegrationEvent> context)
        {
            await _thumbnailsService.GenerateThumbnails(context.Message.ImageId, context.Message.ImageGroup);
        }
    }
}