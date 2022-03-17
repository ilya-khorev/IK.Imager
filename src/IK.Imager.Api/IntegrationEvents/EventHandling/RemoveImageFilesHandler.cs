using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.ImageDeleting;
using MassTransit;
using MediatR;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling
{
    /// <summary>
    /// Removing files of the original image and its thumbnails.
    /// Metadata object has been already removed before this event was delivered.
    /// </summary>
    public class RemoveImageFilesHandler: IConsumer<ImageMetadataDeletedIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public RemoveImageFilesHandler(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        public async Task Consume(ConsumeContext<ImageMetadataDeletedIntegrationEvent> context)
        {
            await _mediator.Send(new DeleteImageCommand(context.Message.ImageId, context.Message.ImageName, context.Message.ThumbnailNames));
        }
    }
}