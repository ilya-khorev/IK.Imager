using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Thumbnails;
using MassTransit;
using Microsoft.Extensions.Logging;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

public class CreateThumbnailsConsumer(
    IThumbnailGenerator thumbnailGenerator,
    ILogger<CreateThumbnailsConsumer> logger) : IConsumer<OriginalImageUploadedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OriginalImageUploadedIntegrationEvent> context)
    {
        //the scope is what carries the image id into ThumbnailGenerator, which runs off the bus on another
        //thread - the trace id already ties the two together, but it does not say which image they are about
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["ImageId"] = context.Message.ImageId,
            ["ImageGroup"] = context.Message.ImageGroup
        });

        logger.ThumbnailJobReceived(context.Message.ImageId, context.Message.ImageGroup);

        await thumbnailGenerator.Generate(context.Message.ImageId, context.Message.ImageGroup, context.CancellationToken);
    }
}
