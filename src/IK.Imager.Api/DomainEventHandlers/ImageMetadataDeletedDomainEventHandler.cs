using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Utils;
using MassTransit;
#pragma warning disable CS1591

namespace IK.Imager.Api.DomainEventHandlers;

public class ImageMetadataDeletedDomainEventHandler: IDomainEventHandler<ImageMetadataDeletedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ImageMetadataDeletedDomainEventHandler(IPublishEndpoint publishEndpoint)
    {
        ArgumentHelper.AssertNotNull(nameof(publishEndpoint), publishEndpoint);
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(ImageMetadataDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await _publishEndpoint.Publish(new ImageMetadataDeletedIntegrationEvent
        {
            ImageId = domainEvent.ImageId,
            ImageName = domainEvent.ImageName,
            ThumbnailNames = domainEvent.ThumbnailNames
        }, cancellationToken);
    }
}
