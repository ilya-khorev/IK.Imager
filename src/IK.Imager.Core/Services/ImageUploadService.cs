using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Services;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Services
{
    public class ImageUploadService: IImageUploadService
    {
        private readonly ILogger<ImageUploadService> _logger;
        private readonly IImageMetadataReader _metadataReader;
        private readonly IImageBlobRepository _blobRepository;
        private readonly IImageMetadataRepository _metadataRepository;
        private readonly IImageValidator _imageValidator;
        private readonly IImageIdentifierProvider _imageIdentifierProvider;
        private readonly ICdnService _cdnService;

        private const string CheckingImage = "Starting to check the image.";
        private const string UploadedToBlobStorage = "Uploaded the image to the blob storage, imageId={0}.";
        private const string UploadingFinished = "Image with id={0} and its metadata have been saved.";
        
        public ImageUploadService(ILogger<ImageUploadService> logger, IImageMetadataReader metadataReader, IImageBlobRepository blobRepository, 
            IImageMetadataRepository metadataRepository, IImageValidator imageValidator, IImageIdentifierProvider imageIdentifierProvider, ICdnService cdnService)
        {
            _logger = logger;
            _metadataReader = metadataReader;
            _blobRepository = blobRepository;
            _metadataRepository = metadataRepository;
            _imageValidator = imageValidator;
            _imageIdentifierProvider = imageIdentifierProvider;
            _cdnService = cdnService;
        }
        
        public async Task<ImageInfo> UploadImage(Stream imageStream, string imageGroup)
        {
             _logger.LogDebug(CheckingImage);
            var imageFormat = _metadataReader.DetectFormat(imageStream); 
            _imageValidator.CheckFormat(imageFormat);
            _logger.LogDebug(imageFormat.ToString());

            var imageSize = _metadataReader.ReadSize(imageStream);
            _imageValidator.CheckSize(imageSize);
            _logger.LogDebug(imageSize.ToString());

            //Firstly, saving the image stream to the blob storage
            string imageId = _imageIdentifierProvider.GenerateUniqueId();
            string imageName = _imageIdentifierProvider.GetImageName(imageId, imageFormat.FileExtension);
            
            //todo check if such name already exist (it's unlikely, but worth checking)
            
            var uploadImageResult = await _blobRepository.UploadImage(imageName, imageStream, ImageSizeType.Original, imageFormat.MimeType, CancellationToken.None);
            _logger.LogDebug(UploadedToBlobStorage, imageId);
            
            //Image stream is no longer needed at this stage
            imageStream.Dispose();
            
            /*
             Next, saving the metadata object of this image
            
             If the program unexpectedly fails at this stage, there will be just a blob file, not connected to any metadata object. In this case,
             the image itself will be unavailable to the clients. And in most cases it is just fine, so an additional handling is not needed here.
            */
            await _metadataRepository.SetMetadata(new ImageMetadata
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
                ImageGroup = imageGroup 
            }, CancellationToken.None);
            
            _logger.LogInformation(string.Format(UploadingFinished, imageId));

            return new ImageInfo
            {
                Id = imageId,
                Name = imageName,
                Hash = uploadImageResult.MD5Hash,
                DateAdded = uploadImageResult.DateAdded,
                Url = _cdnService.TryTransformToCdnUri(uploadImageResult.Url).ToString(),
                Bytes = imageSize.Bytes,
                Height = imageSize.Height,
                Width = imageSize.Width,
                MimeType = imageFormat.MimeType
            };
        }
    }
}