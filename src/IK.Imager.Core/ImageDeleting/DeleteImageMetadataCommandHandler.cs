using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageDeleting;

public class DeleteImageMetadataCommandHandler: IRequestHandler<DeleteImageMetadataCommand, bool>
{
    private readonly ILogger<DeleteImageMetadataCommandHandler> _logger;
    private readonly IImageMetadataRepository _metadataRepository;
    private readonly IMediator _mediator;

    private const string MetadataRemoving = "Removing metadata of imageId = {0}, imageGroup = {1}";
    private const string MetadataRemoved = "Metadata removed for imageId = {0}";

    public DeleteImageMetadataCommandHandler(ILogger<DeleteImageMetadataCommandHandler> logger, IImageMetadataRepository metadataRepository, IMediator mediator)
    {
        _logger = logger;
        _metadataRepository = metadataRepository;
        _mediator = mediator;
    }
        
    public async Task<bool> Handle(DeleteImageMetadataCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(MetadataRemoving, request.ImageId, request.ImageGroup);
            
        var metadata = await _metadataRepository.GetMetadata(new List<string> {request.ImageId}, request.ImageGroup, CancellationToken.None);
        if (metadata == null || !metadata.Any())
            return false;
            
        var imageMetadata = metadata[0];
            
        var deletedMetadata = await _metadataRepository.RemoveMetadata(imageMetadata.Id, imageMetadata.ImageGroup, CancellationToken.None);
        if (!deletedMetadata)
            return false;

        _logger.LogInformation(MetadataRemoved, request.ImageId);

        await _mediator.Publish(new ImageMetadataDeletedDomainEvent(imageMetadata.Id, imageMetadata.Name,
            imageMetadata.Thumbnails != null ? imageMetadata.Thumbnails.Select(x => x.Name).ToArray() : Array.Empty<string>()));
        
        return true;
    }
}