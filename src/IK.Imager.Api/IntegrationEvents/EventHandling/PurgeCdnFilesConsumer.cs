using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Storage.Abstractions.Models;
using MassTransit;
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
public class PurgeCdnFilesConsumer : IConsumer<ImageFilesDeletedIntegrationEvent>
{
    private readonly IImageUrlBuilder _imageUrlBuilder;
    private readonly ICdnPurger _cdnPurger;

    public PurgeCdnFilesConsumer(IImageUrlBuilder imageUrlBuilder, ICdnPurger cdnPurger)
    {
        _imageUrlBuilder = imageUrlBuilder;
        _cdnPurger = cdnPurger;
    }

    public async Task Consume(ConsumeContext<ImageFilesDeletedIntegrationEvent> context)
    {
        var thumbnailNames = context.Message.ThumbnailNames;

        var contentUris = new List<Uri>(thumbnailNames.Length + 1)
        {
            _imageUrlBuilder.Build(context.Message.ImageName, ImageVariant.Original)
        };

        foreach (var thumbnailName in thumbnailNames)
            contentUris.Add(_imageUrlBuilder.Build(thumbnailName, ImageVariant.Thumbnail));

        await _cdnPurger.Purge(contentUris, context.CancellationToken);
    }
}
