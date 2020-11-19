using System;
using System.Collections.Generic;
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
            _blobStorage = new InMemoryMockedImageBlobStorage();
            _metadataStorage = new InMemoryMockedImageMetadataStorage();
            _imageDeleteService = new ImageDeleteService(output.BuildLoggerFor<ImageDeleteService>(), _metadataStorage, _blobStorage);
            
            var imageIdentifierProvider = new ImageIdentifierProvider();
            IImageMetadataReader imageMetadataReader = new ImageMetadataReader();
            IImageValidator imageValidator = new MockImageValidator();
            _imageUploadService = new ImageUploadService(output.BuildLoggerFor<ImageUploadService>(), imageMetadataReader, _blobStorage,
                _metadataStorage, imageValidator, imageIdentifierProvider, new MockCdnService());
        }
        
        [Fact]
        public async Task ShouldRemoveMetadataOnly()
        {
            var imageGroup = Guid.NewGuid().ToString();
            var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(imageGroup, _imageUploadService);
            var deletionResult = await _imageDeleteService.DeleteImageMetadata(uploadedImage.Id, imageGroup);
            
            Assert.Equal(uploadedImage.Id, deletionResult.ImageId);

            var metadata = await _metadataStorage.GetMetadata(new List<string>() {uploadedImage.Id}, imageGroup, CancellationToken.None);
            Assert.False(metadata.Any());

            var imageExists = await _blobStorage.ImageExists(deletionResult.ImageName, ImageSizeType.Original,
                CancellationToken.None);
            Assert.True(imageExists);
        }
        
        [Fact]
        public async Task ShouldRemoveImageAndItsThumbnails()
        {
            var imageGroup = Guid.NewGuid().ToString();
            var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(imageGroup, _imageUploadService);
            await _imageDeleteService.DeleteImageAndThumbnails(new ImageShortInfo()
            {
                ImageId = uploadedImage.Id,
                ImageName = uploadedImage.Name,
                ThumbnailNames = new string[0]
            });
            
            var imageExists = await _blobStorage.ImageExists(uploadedImage.Name, ImageSizeType.Original, CancellationToken.None);
            Assert.False(imageExists);
        }
    }
}