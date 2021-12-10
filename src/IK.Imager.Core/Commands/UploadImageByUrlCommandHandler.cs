using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using MediatR;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.Commands;

public class UploadImageByUrlCommandHandler: IRequestHandler<UploadImageByUrlCommand, ImageInfo>
{
    private readonly ImageDownloadClient _imageDownloadClient;
    private readonly IMediator _mediator;
    private readonly ILogger<UploadImageByUrlCommandHandler> _logger;

    private const string DownloadingByUrl = "Downloading an image by url {0}.";
    private const string DownloadedByUrl = "Downloaded an image by url {0}.";
        
    public UploadImageByUrlCommandHandler(ImageDownloadClient imageDownloadClient, IMediator mediator, ILogger<UploadImageByUrlCommandHandler> logger)
    {
        _imageDownloadClient = imageDownloadClient;
        _mediator = mediator;
        _logger = logger;
    }
        
    public async Task<ImageInfo> Handle(UploadImageByUrlCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(DownloadingByUrl, request.ImageUrl);
            
        var imageStream = await _imageDownloadClient.GetMemoryStream(request.ImageUrl);
        if (imageStream == null)
        {
            //todo handle
        }
            
        _logger.LogDebug(DownloadedByUrl, request.ImageUrl);
            
        var uploadImageResult = await _mediator.Send(new UploadImageCommand(imageStream, request.ImageGroup), cancellationToken);
        return uploadImageResult;
    }
}