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
        private readonly List<int> _thumbnailTargetWidths;

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
            _thumbnailTargetWidths = imageThumbnailsSettings.Value.TargetWidth.OrderByDescending(x => x).ToList();
        }
        
        //todo 
        /*
         * "width": 199,
          "height": 132,
         */
        
        /// <inheritdoc />
        public async Task CreateThumbnails(string imageId, string imageGroup)
        {
            //firstly, receiving image metadata of the given image
            var imageMetadataList = await _metadataRepository.GetMetadata(new List<string> {imageId}, imageGroup, CancellationToken.None);
            if (imageMetadataList == null || !imageMetadataList.Any())
            {
                _logger.LogInformation(ImageNotFound, imageId);
                return;
            }

            var imageMetadata = imageMetadataList[0];
            imageMetadata.Thumbnails = new List<ImageThumbnail>();
            _logger.LogDebug(MetadataReceived, imageMetadata.Id, imageMetadata.Width, imageMetadata.Height);
            if (imageMetadata.Width <= _thumbnailTargetWidths.Last())
            {
                _logger.LogInformation(ImageSmallerThanTargetWidth, imageMetadata.Id, imageMetadata.Width);
                return;
            }

            await using var originalImageStream = await _blobRepository.DownloadImage(imageMetadata.Name, ImageSizeType.Original, CancellationToken.None);
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
            foreach (var targetWidth in _thumbnailTargetWidths)
            {
                //the current image width is smaller than the target thumbnail with, so just ignoring it 
                //and moving to the next target thumbnail
                if (targetWidth >= imageMetadata.Width)
                    continue;

                var resizingResult = _imageResizing.Resize(imageStream, (ImageType) imageType, targetWidth);
                _logger.LogDebug(ImageResized, imageMetadata.Id, resizingResult.Size);

                var thumbnailImageId = _imageIdentifierProvider.GenerateUniqueId();
                var thumbnailImageName = _imageIdentifierProvider.GetImageFileName(thumbnailImageId, fileExtension);
                
                //todo use cancellation token
                
                var uploadedBlob = await _blobRepository.UploadImage(thumbnailImageName, resizingResult.Image, ImageSizeType.Thumbnail, mimeType, CancellationToken.None);
                imageMetadata.Thumbnails.Add(new ImageThumbnail
                {
                    Id = thumbnailImageId,
                    Name = thumbnailImageName,
                    MD5Hash = uploadedBlob.Hash,
                    DateAddedUtc = uploadedBlob.DateAdded.DateTime,
                    MimeType = mimeType,
                    Height = resizingResult.Size.Height,
                    Width = resizingResult.Size.Width,
                    SizeBytes = resizingResult.Size.Bytes
                });

                //keeping reference to the resized image, so that the further thumbnail is generated faster
                imageStream = resizingResult.Image;
            }

            await imageStream.DisposeAsync();
            
            imageMetadata.Thumbnails.Reverse(); //smaller thumbnails come first
            await _metadataRepository.SetMetadata(imageMetadata, CancellationToken.None);
            _logger.LogInformation(ThumbnailsGenerated, imageMetadata.Thumbnails.Count, imageId);
        }
    }
}