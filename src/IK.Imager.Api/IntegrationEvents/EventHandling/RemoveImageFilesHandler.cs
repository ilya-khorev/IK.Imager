using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.ImageDeleting;
using IK.Imager.Core.Abstractions.Models;
using MassTransit;

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling
{
    /// <summary>
    /// Removing files of the original image and its thumbnails.
    /// Metadata object has been already removed before this event was delivered.
    /// </summary>
    public class RemoveImageFilesHandler: IConsumer<ImageDeletedIntegrationEvent>
    {
        private readonly IImageDeleteService _imageDeleteService;

        public RemoveImageFilesHandler(IImageDeleteService imageDeleteService)
        {
            _imageDeleteService = imageDeleteService;
        }
        
        public async Task Consume(ConsumeContext<ImageDeletedIntegrationEvent> context)
        {
            await _imageDeleteService.DeleteImageAndThumbnails(new ImageShortInfo
            {
                ImageId = context.Message.ImageId,
                ImageName = context.Message.ImageName,
                ThumbnailNames = context.Message.ThumbnailNames
            });
        }
    }
}