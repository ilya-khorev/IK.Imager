using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.ImageRemoving;
using MassTransit;
using MediatR;

namespace IK.Imager.Api.DomainEventHandlers;

public class ImageRemovedDomainEventHandler: INotificationHandler<ImageRemovedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ImageRemovedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }
    
    public async Task Handle(ImageRemovedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _publishEndpoint.Publish(new ImageDeletedIntegrationEvent
        {
            ImageId = notification.ImageId,
            ImageName = notification.ImageName,
            ThumbnailNames = notification.ThumbnailNames
        });
    }
}