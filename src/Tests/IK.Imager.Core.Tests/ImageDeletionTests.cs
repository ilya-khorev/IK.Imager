using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.ImagesCrud;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.ImagesCrud;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests
{
    public class ImageDeletionTests
    {
        private readonly IImageBlobRepository _blobRepository;
        private readonly IImageMetadataRepository _metadataRepository;
        private readonly IImageDeleteService _imageDeleteService;
        private readonly ImageUploadService _imageUploadService;

        public ImageDeletionTests(ITestOutputHelper output)
        {
            _blobRepository = new InMemoryMockedImageBlobRepository();
            _metadataRepository = new InMemoryMockedImageMetadataRepository();
            _imageDeleteService = new ImageDeleteService(output.BuildLoggerFor<ImageDeleteService>(), _metadataRepository, _blobRepository);
            
            var imageIdentifierProvider = new ImageIdentifierProvider();
            IImageMetadataReader imageMetadataReader = new ImageMetadataReader();
            IImageValidator imageValidator = new MockImageValidator();
            _imageUploadService = new ImageUploadService(output.BuildLoggerFor<ImageUploadService>(), imageMetadataReader, _blobRepository,
                _metadataRepository, imageValidator, imageIdentifierProvider, new MockCdnService());
        }
        
        [Fact]
        public async Task ShouldRemoveMetadataOnly()
        {
            var imageGroup = Guid.NewGuid().ToString();
            var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(imageGroup, _imageUploadService);
            var deletionResult = await _imageDeleteService.DeleteImageMetadata(uploadedImage.Id, imageGroup);
            
            Assert.Equal(uploadedImage.Id, deletionResult.ImageId);

            var metadata = await _metadataRepository.GetMetadata(new List<string>() {uploadedImage.Id}, imageGroup, CancellationToken.None);
            Assert.False(metadata.Any());

            var imageExists = await _blobRepository.ImageExists(deletionResult.ImageName, ImageSizeType.Original,
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
            
            var imageExists = await _blobRepository.ImageExists(uploadedImage.Name, ImageSizeType.Original, CancellationToken.None);
            Assert.False(imageExists);
        }
    }
}