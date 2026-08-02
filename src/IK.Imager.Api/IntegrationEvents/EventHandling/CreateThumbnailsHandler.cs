using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Utils;
using MassTransit;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

public class CreateThumbnailsHandler : IConsumer<OriginalImageUploadedIntegrationEvent>
{
    private readonly ICommandHandler<CreateThumbnailsCommand> _createThumbnailsCommandHandler;

    public CreateThumbnailsHandler(ICommandHandler<CreateThumbnailsCommand> createThumbnailsCommandHandler)
    {
        ArgumentHelper.AssertNotNull(nameof(createThumbnailsCommandHandler), createThumbnailsCommandHandler);
        _createThumbnailsCommandHandler = createThumbnailsCommandHandler;
    }

    public async Task Consume(ConsumeContext<OriginalImageUploadedIntegrationEvent> context)
    {
        await _createThumbnailsCommandHandler.Handle(
            new CreateThumbnailsCommand(context.Message.ImageId, context.Message.ImageGroup), context.CancellationToken);
    }
}
