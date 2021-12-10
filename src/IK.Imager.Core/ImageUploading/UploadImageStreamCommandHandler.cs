using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.ImageUploading;
using IK.Imager.Core.Abstractions.Models;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageUploading;

public class UploadImageCommandHandler: IRequestHandler<UploadImageCommand, ImageInfo>
{
    private readonly IImageUploadService _imageUploadService;
    private readonly IMediator _mediator;

    public UploadImageCommandHandler(IImageUploadService imageUploadService, IMediator mediator)
    {
        _imageUploadService = imageUploadService;
        _mediator = mediator;
    }
        
    public async Task<ImageInfo> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        var uploadImageResult = await _imageUploadService.UploadImage(request.ImageStream, request.ImageGroup);
        await _mediator.Publish(new ImageUploadedDomainEvent(uploadImageResult.Id, request.ImageGroup));
        return uploadImageResult;
    }
}