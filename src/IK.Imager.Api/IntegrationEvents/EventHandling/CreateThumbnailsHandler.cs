using System.Threading.Tasks;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Utils;
using MassTransit;
using MediatR;
// ReSharper disable ClassNeverInstantiated.Global

#pragma warning disable 1591

namespace IK.Imager.Api.IntegrationEvents.EventHandling;

public class CreateThumbnailsHandler : IConsumer<OriginalImageUploadedIntegrationEvent>
{
    private readonly IMediator _mediator;
        
    public CreateThumbnailsHandler(IMediator mediator)
    {
        ArgumentHelper.AssertNotNull(nameof(mediator), mediator);
        _mediator = mediator;
    }
        
    public async Task Consume(ConsumeContext<OriginalImageUploadedIntegrationEvent> context)
    {
        await _mediator.Send(new CreateThumbnailsCommand(context.Message.ImageId, context.Message.ImageGroup));
    }
}