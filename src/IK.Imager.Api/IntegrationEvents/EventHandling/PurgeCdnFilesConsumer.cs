using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Storage.Abstractions.Models;
using MassTransit;
using Microsoft.Extensions.Logging;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

/// <summary>
/// Purging the deleted image and its thumbnails from the CDN.
/// </summary>
/// <remarks>
/// A reaction to the deletion rather than a step inside it, so that a slow or rate limited purge retries
/// on its own queue instead of re-running the blob removal, and cannot hold up the delete subscription.
/// Does nothing until a provider module registers a real <see cref="ICdnPurger"/>.
/// </remarks>
public class PurgeCdnFilesConsumer(
    IImageUrlBuilder imageUrlBuilder,
    ICdnPurger cdnPurger,
    ILogger<PurgeCdnFilesConsumer> logger) : IConsumer<ImageFilesDeletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ImageFilesDeletedIntegrationEvent> context)
    {
        //the purger has no idea what an image is, so the scope is how its lines get an image id
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ImageId"] = context.Message.ImageId
        });

        var thumbnailNames = context.Message.ThumbnailNames;

        var contentUris = new List<Uri>(thumbnailNames.Length + 1)
        {
            imageUrlBuilder.Build(context.Message.ImageName, ImageVariant.Original)
        };

        foreach (var thumbnailName in thumbnailNames)
            contentUris.Add(imageUrlBuilder.Build(thumbnailName, ImageVariant.Thumbnail));

        logger.PurgeJobReceived(contentUris.Count, context.Message.ImageId);

        await cdnPurger.Purge(contentUris, context.CancellationToken);
    }
}
