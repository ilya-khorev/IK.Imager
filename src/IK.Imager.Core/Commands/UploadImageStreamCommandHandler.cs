using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.Commands;

public class UploadImageCommandHandler: IRequestHandler<UploadImageCommand, ImageInfo>
{
    private readonly IImageUploadService _imageUploadService;

    public UploadImageCommandHandler(IImageUploadService imageUploadService)
    {
        _imageUploadService = imageUploadService;
    }
        
    public async Task<ImageInfo> Handle(UploadImageCommand request, CancellationToken cancellationToken)
    {
        var uploadImageResult = await _imageUploadService.UploadImage(request.ImageStream, request.ImageGroup);
        return uploadImageResult;
    }
}