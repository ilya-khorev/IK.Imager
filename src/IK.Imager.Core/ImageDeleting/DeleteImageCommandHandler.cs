using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.ImageDeleting;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.ImageDeleting;

public class DeleteImageCommandHandler: IRequestHandler<DeleteImageCommand>
{
    private readonly ILogger<ImageDeleteService> _logger;
    private readonly IImageBlobRepository _blobRepository;
    
    private const string Removing = "Removing image and thumbnails for {0}";
    private const string OriginalImageDeleted = "Original image {0} has been deleted. ";
    private const string ThumbnailsDeleted = "{0} / {1} thumbnails were deleted.";
    
    public DeleteImageCommandHandler(ILogger<ImageDeleteService> logger, IImageBlobRepository blobRepository)
    {
        _logger = logger;
        _blobRepository = blobRepository;
    }
    
    public async Task<Unit> Handle(DeleteImageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(Removing, request);
            
        bool originalImageDeleted = await _blobRepository.TryDeleteImage(request.ImageName, ImageSizeType.Original, CancellationToken.None);
        int deletedThumbnails = 0; 
        foreach (var thumbnailName in request.ThumbnailNames)
        {
            if (await _blobRepository.TryDeleteImage(thumbnailName, ImageSizeType.Thumbnail, CancellationToken.None))
                deletedThumbnails++;
        }
            
        StringBuilder stringBuilder = new StringBuilder();

        if (originalImageDeleted)
            stringBuilder.AppendFormat(OriginalImageDeleted, request.ImageId);

        stringBuilder.AppendFormat(ThumbnailsDeleted, request.ThumbnailNames.Length, deletedThumbnails);
        _logger.LogInformation(stringBuilder.ToString());
        return Unit.Value;
    }
}