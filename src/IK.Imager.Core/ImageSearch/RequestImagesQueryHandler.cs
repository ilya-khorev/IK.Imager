using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using MediatR;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageSearch;

public class RequestImagesQueryHandler: IRequestHandler<RequestImagesQuery, ImagesSearchResult>
{
    private readonly ILogger<RequestImagesQueryHandler> _logger;
    private readonly IImageMetadataRepository _metadataRepository;
    private readonly IImageBlobRepository _blobRepository;
    private const string FoundImages = "Found {0} image(s) for requested {1} image id(s)";
    
    public RequestImagesQueryHandler(ILogger<RequestImagesQueryHandler> logger, IImageMetadataRepository metadataRepository, IImageBlobRepository blobRepository)
    {
        _logger = logger;
        _metadataRepository = metadataRepository;
        _blobRepository = blobRepository;
    }
        
    public async Task<ImagesSearchResult> Handle(RequestImagesQuery request, CancellationToken cancellationToken)
    {
        var imagesMetadata = await _metadataRepository.GetMetadata(request.ImageIds, request.ImageGroup, CancellationToken.None);

        ImagesSearchResult result = new ImagesSearchResult
        {
            Images = new List<ImageFullInfoWithThumbnails>(imagesMetadata.Count)
        };

        foreach (var imageMetadata in imagesMetadata)
        {
            var model = new ImageFullInfoWithThumbnails
            {
                Id = imageMetadata.Id,
                Bytes = imageMetadata.SizeBytes,
                Hash = imageMetadata.MD5Hash,
                Height = imageMetadata.Height,
                Width = imageMetadata.Width,
                Tags = imageMetadata.Tags ?? new Dictionary<string, string>(),
                Url = _blobRepository.GetImageUri(imageMetadata.Name, ImageSizeType.Original),
                DateAdded = imageMetadata.DateAddedUtc,
                MimeType = imageMetadata.MimeType,
                Thumbnails = new List<ImageInfo>()
            };

            //todo if an image was added a long time ago and there are not any thumbnails, it's worth sending a new event to generate them
                
            if (imageMetadata.Thumbnails != null)
                foreach (var thumbnail in imageMetadata.Thumbnails)
                {
                    model.Thumbnails.Add(new ImageInfo
                    {
                        Id = thumbnail.Id,
                        Bytes = thumbnail.SizeBytes,
                        Hash = thumbnail.MD5Hash,
                        Height = thumbnail.Height,
                        Width = thumbnail.Width,
                        DateAdded = thumbnail.DateAddedUtc,
                        MimeType = thumbnail.MimeType,
                        Url = _blobRepository.GetImageUri(thumbnail.Name, ImageSizeType.Thumbnail)
                    });
                }
                    
            result.Images.Add(model);
        }
            
        _logger.LogInformation(FoundImages, result.Images.Count, request.ImageIds.Length);
        
        return result;
    }
}