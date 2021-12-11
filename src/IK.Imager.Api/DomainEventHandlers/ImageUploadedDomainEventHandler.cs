using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.ImageUploading;
using IK.Imager.Utils;
using MassTransit;
using MediatR;

namespace IK.Imager.Api.DomainEventHandlers;

public class ImageUploadedDomainEventHandler: INotificationHandler<ImageUploadedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ImageUploadedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        ArgumentHelper.AssertNotNull(nameof(publishEndpoint), publishEndpoint);
        _publishEndpoint = publishEndpoint;
    }
    
    public async Task Handle(ImageUploadedDomainEvent notification, CancellationToken cancellationToken)
    {
        //Once the image file and metadata object are saved, there is time to send a new message to the event bus topic
        //If the program fails at this stage, this message is not sent and therefore thumbnails are not generated for the image. 
        //Such cases are handled when requesting an image metadata object later by resending this event again.
            
        await _publishEndpoint.Publish(new OriginalImageUploadedIntegrationEvent
        {
            ImageId = notification.ImageId,
            ImageGroup = notification.ImageGroup
        });
    }
}