using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.IntegrationEvents;
using MassTransit;

#pragma warning disable 1591

namespace IK.Imager.Api.Handlers
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