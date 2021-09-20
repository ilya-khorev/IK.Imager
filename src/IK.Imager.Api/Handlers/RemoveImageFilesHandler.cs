using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Services;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.IntegrationEvents;
#pragma warning disable 1591

namespace IK.Imager.Api.Handlers
{
    /// <summary>
    /// Removing files of the original image and its thumbnails.
    /// Metadata object has been already removed before this event was delivered.
    /// </summary>
    public class RemoveImageFilesHandler: IIntegrationEventHandler<ImageDeletedIntegrationEvent>
    {
        private readonly IImageDeleteService _imageDeleteService;

        public RemoveImageFilesHandler(IImageDeleteService imageDeleteService)
        {
            _imageDeleteService = imageDeleteService;
        }
        
        public async Task Handle(ImageDeletedIntegrationEvent iEvent)
        {
            await _imageDeleteService.DeleteImageAndThumbnails(new ImageShortInfo
            {
                ImageId = iEvent.ImageId,
                ImageName = iEvent.ImageName,
                ThumbnailNames = iEvent.ThumbnailNames
            });
        }
    }
}