using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Delete;
using MassTransit;
using Microsoft.Extensions.Logging;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

/// <summary>
/// Removing files of the original image and its thumbnails.
/// Metadata object has been already removed before this event was delivered.
/// </summary>
public class RemoveImageFilesConsumer(
    IImageDeleter imageDeleter,
    ILogger<RemoveImageFilesConsumer> logger) : IConsumer<ImageMetadataDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ImageMetadataDeletedIntegrationEvent> context)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ImageId"] = context.Message.ImageId
        });

        logger.RemoveFilesJobReceived(context.Message.ImageId, context.Message.ThumbnailBlobPaths.Length);

        await imageDeleter.DeleteFiles(context.Message.ImageId, context.Message.BlobPath,
            context.Message.ThumbnailBlobPaths, context.CancellationToken);

        //published only after the blobs are gone - a CDN purge that runs while they still exist just makes
        //the edge fetch them again
        await context.Publish(new ImageFilesDeletedIntegrationEvent(context.Message.ImageId,
            context.Message.BlobPath, context.Message.ThumbnailBlobPaths));
    }
}
