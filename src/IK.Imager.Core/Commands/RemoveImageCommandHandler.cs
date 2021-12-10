using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.Commands;

public class RemoveImageCommandHandler: IRequestHandler<RemoveImageCommand, RemoveImageResult>
{
    private readonly IImageDeleteService _imageDeleteService;
        
    public RemoveImageCommandHandler(IImageDeleteService imageDeleteService)
    {
        _imageDeleteService = imageDeleteService;
    }
        
    public async Task<RemoveImageResult> Handle(RemoveImageCommand request, CancellationToken cancellationToken)
    {
        var imageDeleteResult = await _imageDeleteService.DeleteImageMetadata(request.ImageId, request.ImageGroup);
        if (imageDeleteResult == null)
            return null;

        return new RemoveImageResult(imageDeleteResult.ImageId, imageDeleteResult.ImageName,
            imageDeleteResult.ThumbnailNames);
    }
}