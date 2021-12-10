using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.ImageRemoving;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageRemoving;

public class RemoveImageCommandHandler: IRequestHandler<RemoveImageCommand, bool>
{
    private readonly IImageDeleteService _imageDeleteService;
    private readonly IMediator _mediator;

    public RemoveImageCommandHandler(IImageDeleteService imageDeleteService, IMediator mediator)
    {
        _imageDeleteService = imageDeleteService;
        _mediator = mediator;
    }
        
    public async Task<bool> Handle(RemoveImageCommand request, CancellationToken cancellationToken)
    {
        var imageDeleteResult = await _imageDeleteService.DeleteImageMetadata(request.ImageId, request.ImageGroup);
        if (imageDeleteResult == null)
            return false;

        await _mediator.Publish(new ImageRemovedDomainEvent(imageDeleteResult.ImageId, imageDeleteResult.ImageName,
            imageDeleteResult.ThumbnailNames));
        
        return true;
    }
}