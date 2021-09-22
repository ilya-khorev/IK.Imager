using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.ImagesCrud;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.ImagesCrud
{
    public class ImageSearchService: IImageSearchService
    {
        private readonly ILogger<ImageSearchService> _logger;
        private readonly IImageMetadataRepository _metadataRepository;
        private readonly IImageBlobRepository _blobRepository;
        private readonly ICdnService _cdnService;
        private const string FoundImages = "Found {0} image(s) for requested {1} image id(s)";
        
        public ImageSearchService(ILogger<ImageSearchService> logger, IImageMetadataRepository metadataRepository, IImageBlobRepository blobRepository, ICdnService cdnService)
        {
            _logger = logger;
            _metadataRepository = metadataRepository;
            _blobRepository = blobRepository;
            _cdnService = cdnService;
        }
        
        /// <inheritdoc />
        public async Task<ImagesSearchResult> Search(string[] imageIds, string imageGroup)
        {
            var imagesMetadata = await _metadataRepository.GetMetadata(imageIds, imageGroup, CancellationToken.None);

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
                    Url = _cdnService.TryTransformToCdnUri(_blobRepository.GetImageUri(imageMetadata.Name, ImageSizeType.Original)).ToString(),
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
                            Url = _cdnService.TryTransformToCdnUri(_blobRepository.GetImageUri(thumbnail.Name, ImageSizeType.Thumbnail)).ToString()
                        });
                    }
                    
                result.Images.Add(model);
            }
            
            _logger.LogInformation(FoundImages, result.Images.Count, imageIds.Length);

            return result;
        }
    }
}