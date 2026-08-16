using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Delete;
using MassTransit;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

/// <summary>
/// Removing files of the original image and its thumbnails.
/// Metadata object has been already removed before this event was delivered.
/// </summary>
public class RemoveImageFilesConsumer : IConsumer<ImageMetadataDeletedIntegrationEvent>
{
    private readonly IImageDeleter _imageDeleter;

    public RemoveImageFilesConsumer(IImageDeleter imageDeleter)
    {
        _imageDeleter = imageDeleter;
    }

    public async Task Consume(ConsumeContext<ImageMetadataDeletedIntegrationEvent> context)
    {
        await _imageDeleter.DeleteFiles(context.Message.ImageId, context.Message.ImageName,
            context.Message.ThumbnailNames, context.CancellationToken);
    }
}
