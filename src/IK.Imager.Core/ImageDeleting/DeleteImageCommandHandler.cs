using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.ImageDeleting;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageDeleting;

public class RemoveImageCommandHandler: IRequestHandler<DeleteImageCommand, bool>
{
    private readonly IImageDeleteService _imageDeleteService;
    private readonly IMediator _mediator;

    public RemoveImageCommandHandler(IImageDeleteService imageDeleteService, IMediator mediator)
    {
        _imageDeleteService = imageDeleteService;
        _mediator = mediator;
    }
        
    public async Task<bool> Handle(DeleteImageCommand request, CancellationToken cancellationToken)
    {
        var imageDeleteResult = await _imageDeleteService.DeleteImageMetadata(request.ImageId, request.ImageGroup);
        if (imageDeleteResult == null)
            return false;

        await _mediator.Publish(new ImageDeletedDomainEvent(imageDeleteResult.ImageId, imageDeleteResult.ImageName,
            imageDeleteResult.ThumbnailNames));
        
        return true;
    }
}