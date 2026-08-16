using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Thumbnails;
using MassTransit;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

public class CreateThumbnailsConsumer(IThumbnailGenerator thumbnailGenerator) : IConsumer<OriginalImageUploadedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OriginalImageUploadedIntegrationEvent> context)
    {
        await thumbnailGenerator.Generate(context.Message.ImageId, context.Message.ImageGroup, context.CancellationToken);
    }
}
