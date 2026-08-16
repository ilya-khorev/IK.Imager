using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions;
using MassTransit;

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents;

/// <summary>
/// The single place where something the core announced becomes a message on Azure Service Bus. It lives in
/// the API rather than in IK.Imager.Core, which is what keeps MassTransit out of the core project.
/// </summary>
public class ImageEventPublisher : IImageEvents
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ImageEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task ImageUploaded(string imageId, string imageGroup, CancellationToken cancellationToken)
    {
        //Once the image file and metadata object are saved, there is time to send a new message to the event bus topic
        //If the program fails at this stage, this message is not sent and therefore thumbnails are not generated for the image.
        //Such cases are handled when requesting an image metadata object later by resending this event again.

        await _publishEndpoint.Publish(new OriginalImageUploadedIntegrationEvent(imageId, imageGroup), cancellationToken);
    }

    public async Task ImageMetadataDeleted(string imageId, string imageName, string[] thumbnailNames, CancellationToken cancellationToken)
    {
        await _publishEndpoint.Publish(new ImageMetadataDeletedIntegrationEvent(imageId, imageName, thumbnailNames), cancellationToken);
    }
}
