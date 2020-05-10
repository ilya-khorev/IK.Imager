using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Services;
using IK.Imager.Core.Services;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests
{
    public class ImageDeletionTests
    {
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IImageDeleteService _imageDeleteService;
        private readonly ImageUploadService _imageUploadService;

        public ImageDeletionTests(ITestOutputHelper output)
        {
            _blobStorage = new MockImageBlobStorage();
            _metadataStorage = new MockImageMetadataStorage();
            _imageDeleteService = new ImageDeleteService(output.BuildLoggerFor<ImageDeleteService>(), _metadataStorage, _blobStorage);
            
            var imageIdentifierProvider = new ImageIdentifierProvider();
            IImageMetadataReader imageMetadataReader = new ImageMetadataReader();
            IImageValidator imageValidator = new MockImageValidator();
            _imageUploadService = new ImageUploadService(output.BuildLoggerFor<ImageUploadService>(), imageMetadataReader, _blobStorage,
                _metadataStorage, imageValidator, imageIdentifierProvider);
        }
        
        [Fact]
        public async Task ShouldRemoveMetadataOnly()
        {
            var partitionKey = Guid.NewGuid().ToString();
            var uploadedImage = await UploadImage(partitionKey);
            var deletionResult = await _imageDeleteService.DeleteImageMetadata(uploadedImage.Id, partitionKey);
            
            Assert.Equal(uploadedImage.Id, deletionResult.ImageId);

            var metadata = await _metadataStorage.GetMetadata(new List<string>() {uploadedImage.Id}, partitionKey, CancellationToken.None);
            Assert.False(metadata.Any());

            var imageExists = await _blobStorage.ImageExists(deletionResult.ImageName, ImageSizeType.Original,
                CancellationToken.None);
            Assert.True(imageExists);
        }
        
        [Fact]
        public async Task ShouldRemoveImageAndItsThumbnails()
        {
            var partitionKey = Guid.NewGuid().ToString();
            var uploadedImage = await UploadImage(partitionKey); 
            await _imageDeleteService.DeleteImageAndThumbnails(new ImageShortInfo()
            {
                ImageId = uploadedImage.Id,
                ImageName = uploadedImage.Name,
                ThumbnailNames = new string[0]
            });
            
            var imageExists = await _blobStorage.ImageExists(uploadedImage.Name, ImageSizeType.Original, CancellationToken.None);
            Assert.False(imageExists);
        }
        
        private async Task<ImageInfo> UploadImage(string partitionKey)
        {
            await using FileStream file = ImageTestsHelper.OpenFileForReading("Images\\png\\1060-800x800.png");
            return await _imageUploadService.UploadImage(file, partitionKey);
        }
    }
}