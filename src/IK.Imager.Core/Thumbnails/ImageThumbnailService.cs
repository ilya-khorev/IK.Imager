using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Settings;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageType = IK.Imager.Core.Abstractions.Models.ImageType;
using StorageImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

namespace IK.Imager.Core.Thumbnails
{
    public class ImageThumbnailService: IImageThumbnailService
    {
        private readonly ILogger<ImageThumbnailService> _logger;
        private readonly IImageResizing _imageResizing;
        private readonly IImageBlobRepository _blobRepository;
        private readonly IImageMetadataRepository _metadataRepository;
        private readonly IImageIdentifierProvider _imageIdentifierProvider;
        private readonly List<int> _thumbnailTargetWidth;

        private const string ImageNotFound = "Image metadata object with id = {0} was not found. Stopping to generate thumbnails.";
        private const string MetadataReceived = "Received original image id = {0}, width = {1}, height = {0}";
        private const string ImageDownloaded = "Downloaded original image id = {0} from storage.";
        private const string ImageResized = "Resized image id = {0} with {1}.";
        private const string ThumbnailsGenerated = "Generated {0} thumbnails for image id = {1}.";
        private const string ImageSmallerThanTargetWidth = "Image id = {0}, width = {1} is smaller than the smallest thumbnail width.";

        private const string PngMimeType = "image/png";
        private const string PngFileExtension = ".png";
        
        public ImageThumbnailService(ILogger<ImageThumbnailService> logger, IImageResizing imageResizing,
            IImageBlobRepository blobRepository, IImageMetadataRepository metadataRepository, IImageIdentifierProvider imageIdentifierProvider,
            IOptions<ImageThumbnailsSettings> imageThumbnailsSettings)
        {
            _logger = logger;
            _imageResizing = imageResizing;
            _blobRepository = blobRepository;
            _metadataRepository = metadataRepository;
            _imageIdentifierProvider = imageIdentifierProvider;
            _thumbnailTargetWidth = imageThumbnailsSettings.Value.TargetWidth.OrderByDescending(x => x).ToList();
        }
        
        /// <inheritdoc />
        public async Task<List<ImageThumbnailGeneratingResult>> GenerateThumbnails(string imageId, string imageGroup)
        {
            var imageMetadataList = await _metadataRepository.GetMetadata(new List<string> {imageId}, imageGroup, CancellationToken.None);
            if (imageMetadataList == null || !imageMetadataList.Any())
            {
                _logger.LogInformation(ImageNotFound, imageId);
                return null;
            }

            var imageMetadata = imageMetadataList[0];
            imageMetadata.Thumbnails = new List<ImageThumbnail>();
            _logger.LogDebug(MetadataReceived, imageMetadata.Id, imageMetadata.Width, imageMetadata.Height);
            if (imageMetadata.Width <= _thumbnailTargetWidth.Last())
            {
                _logger.LogInformation(ImageSmallerThanTargetWidth, imageMetadata.Id, imageMetadata.Width);
                return new List<ImageThumbnailGeneratingResult>(0);
            }
            
            using var originalImageStream = await _blobRepository.DownloadImage(imageMetadata.Name, ImageSizeType.Original, CancellationToken.None);
            _logger.LogDebug(ImageDownloaded, imageMetadata.Id);

            StorageImageType imageType = imageMetadata.ImageType;
            string mimeType = imageMetadata.MimeType;
            string fileExtension = imageMetadata.FileExtension;
            if (imageType == StorageImageType.BMP)
            {
                imageType = StorageImageType.PNG;
                mimeType = PngMimeType;
                fileExtension = PngFileExtension;
            }
            
            var imageStream = originalImageStream;
            foreach (var targetWidth in _thumbnailTargetWidth)
            {
                if (targetWidth >= imageMetadata.Width)
                    continue;

                var resizingResult = _imageResizing.Resize(imageStream, (ImageType) imageType, targetWidth);
                _logger.LogDebug(ImageResized, imageMetadata.Id, resizingResult.Size);

                var thumbnailImageId = _imageIdentifierProvider.GenerateUniqueId();
                var thumbnailImageName = _imageIdentifierProvider.GetImageName(thumbnailImageId, fileExtension);
                
                var uploadedBlob = await _blobRepository.UploadImage(thumbnailImageName, resizingResult.Image, ImageSizeType.Thumbnail, mimeType, CancellationToken.None);
                imageMetadata.Thumbnails.Add(new ImageThumbnail
                {
                    Id = thumbnailImageId,
                    Name = thumbnailImageName,
                    MD5Hash = uploadedBlob.MD5Hash,
                    DateAddedUtc = uploadedBlob.DateAdded.DateTime,
                    MimeType = mimeType,
                    Height = resizingResult.Size.Height,
                    Width = resizingResult.Size.Width,
                    SizeBytes = resizingResult.Size.Bytes
                });

                imageStream = resizingResult.Image;
            }

            imageStream.Dispose();
            
            imageMetadata.Thumbnails.Reverse();
            await _metadataRepository.SetMetadata(imageMetadata, CancellationToken.None);
            _logger.LogInformation(ThumbnailsGenerated, imageMetadata.Thumbnails.Count, imageId);
            
            return imageMetadata.Thumbnails.Select(x => new ImageThumbnailGeneratingResult
            {
                Id = x.Id,
                Width = x.Width,
                Height = x.Height,
                MimeType = x.MimeType
            }).ToList();
        }
    }
}