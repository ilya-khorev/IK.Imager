using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.ImageDeleting;
using IK.Imager.Utils;
using MassTransit;
using MediatR;

namespace IK.Imager.Api.DomainEventHandlers;

public class ImageDeletedDomainEventHandler: INotificationHandler<ImageDeletedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ImageDeletedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        ArgumentHelper.AssertNotNull(nameof(publishEndpoint), publishEndpoint);
        _publishEndpoint = publishEndpoint;
    }
    
    public async Task Handle(ImageDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _publishEndpoint.Publish(new ImageDeletedIntegrationEvent
        {
            ImageId = notification.ImageId,
            ImageName = notification.ImageName,
            ThumbnailNames = notification.ThumbnailNames
        });
    }
}