using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.ImageDeleting;
using IK.Imager.Utils;
using MassTransit;
using MediatR;

namespace IK.Imager.Api.DomainEventHandlers;

public class ImageMetadataDeletedDomainEventHandler: INotificationHandler<ImageMetadataDeletedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ImageMetadataDeletedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        ArgumentHelper.AssertNotNull(nameof(publishEndpoint), publishEndpoint);
        _publishEndpoint = publishEndpoint;
    }
    
    public async Task Handle(ImageMetadataDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _publishEndpoint.Publish(new ImageMetadataDeletedIntegrationEvent
        {
            ImageId = notification.ImageId,
            ImageName = notification.ImageName,
            ThumbnailNames = notification.ThumbnailNames
        });
    }
}