using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Utils;
using MassTransit;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

public class CreateThumbnailsHandler : IConsumer<OriginalImageUploadedIntegrationEvent>
{
    private readonly IThumbnailGenerator _thumbnailGenerator;

    public CreateThumbnailsHandler(IThumbnailGenerator thumbnailGenerator)
    {
        ArgumentHelper.AssertNotNull(nameof(thumbnailGenerator), thumbnailGenerator);
        _thumbnailGenerator = thumbnailGenerator;
    }

    public async Task Consume(ConsumeContext<OriginalImageUploadedIntegrationEvent> context)
    {
        await _thumbnailGenerator.Generate(context.Message.ImageId, context.Message.ImageGroup, context.CancellationToken);
    }
}
