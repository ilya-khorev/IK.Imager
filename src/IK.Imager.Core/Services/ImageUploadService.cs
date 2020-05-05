using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Services;
using IK.Imager.Core.Configuration;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core.Services
{
    public class ImageUploadService: IImageUploadService
    {
        private readonly ILogger<ImageUploadService> _logger;
        private readonly IImageMetadataReader _metadataReader;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IOptions<ImageLimitationSettings> _limitationSettings;
        private readonly IImageIdentifierProvider _imageIdentifierProvider;

        private const string UnsupportedFormat = "Unsupported image format. Please use one of the following formats: {0}.";
        private const string IncorrectSize = "Image size must be between {0} and {1} bytes.";
        private const string IncorrectDimensions = "Image width must be between {0} and {1} px. Image height must be between {2} and {3} px.";
        private const string CheckingImage = "Starting to check the image.";
        private const string UploadedToBlobStorage = "Uploaded the image to the blob storage, id={0}.";
        private const string UploadingFinished = "Image {0} and its metadata has been saved.";
        
        public ImageUploadService(ILogger<ImageUploadService> logger, IImageMetadataReader metadataReader, IImageBlobStorage blobStorage, IImageMetadataStorage metadataStorage, 
           IOptions<ImageLimitationSettings> limitationSettings, IImageIdentifierProvider imageIdentifierProvider)
        {
            _logger = logger;
            _metadataReader = metadataReader;
            _blobStorage = blobStorage;
            _metadataStorage = metadataStorage;
            _limitationSettings = limitationSettings;
            _imageIdentifierProvider = imageIdentifierProvider;
        }
        
        public async Task<ImageInfo> UploadImage(Stream imageStream, string partitionKey)
        {
             _logger.LogDebug(CheckingImage);
            var imageFormat = _metadataReader.DetectFormat(imageStream); 
            var limits = _limitationSettings.Value;
            
            if (imageFormat == null || !limits.Types.Contains(imageFormat.ImageType.ToString()))
                throw new ValidationException(string.Format(UnsupportedFormat, string.Join(",", limits.Types)));
            
            _logger.LogDebug(imageFormat.ToString());

            var imageSize = _metadataReader.ReadSize(imageStream);
            _logger.LogDebug(imageSize.ToString());

            string sizeValidationError = ValidateSize(imageSize, limits);
            if (sizeValidationError != null)
                throw new ValidationException(sizeValidationError);
             
            //Firstly, saving the image stream to the blob storage
            string imageId = _imageIdentifierProvider.GenerateUniqueId();
            string imageName = _imageIdentifierProvider.GetImageName(imageId, imageFormat.FileExtension);
            var uploadImageResult = await _blobStorage.UploadImage(imageName, imageStream, ImageSizeType.Original, imageFormat.MimeType, CancellationToken.None);
            _logger.LogDebug(UploadedToBlobStorage, imageId);
            
            //Image stream is no longer needed at this stage
            imageStream.Dispose();
            
            //Next, saving the metadata object of this image
            //When the program unexpectedly fails at this stage, there will be just a blob file not connected to any metadata object
            //and therefore the image will be unavailable to the clients. In most cases it is just fine.
            await _metadataStorage.SetMetadata(new ImageMetadata
            {
                Id = imageId,
                Name = imageName,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MD5Hash = uploadImageResult.MD5Hash,
                SizeBytes = imageSize.Bytes,
                MimeType = imageFormat.MimeType,
                ImageType = (Storage.Abstractions.Models.ImageType) imageFormat.ImageType,
                FileExtension = imageFormat.FileExtension,
                PartitionKey = partitionKey 
            }, CancellationToken.None);
            
            _logger.LogInformation(string.Format(UploadingFinished, imageId));

            return new ImageInfo
            {
                Id = imageId,
                Hash = uploadImageResult.MD5Hash,
                DateAdded = uploadImageResult.DateAdded,
                Url = uploadImageResult.Url.ToString(),
                Bytes = imageSize.Bytes,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MimeType = imageFormat.MimeType
            };
        }
        
        private string ValidateSize(ImageSize imageSize, ImageLimitationSettings limits)
        {
            if (imageSize.Bytes > limits.SizeBytes.Max || imageSize.Bytes < limits.SizeBytes.Min)
                return string.Format(IncorrectSize, limits.SizeBytes.Min, limits.SizeBytes.Max);
            
            if (imageSize.Width > limits.Width.Max || imageSize.Width < limits.Width.Min || 
                imageSize.Height > limits.Height.Max || imageSize.Height < limits.Height.Min)
                return string.Format(IncorrectDimensions, limits.Width.Min, limits.Width.Max, limits.Height.Min, limits.Height.Max);

            //todo extract to a separate class ImageValidator
            //todo add aspect ration restrictions
            
            return null;
        }
    }
}