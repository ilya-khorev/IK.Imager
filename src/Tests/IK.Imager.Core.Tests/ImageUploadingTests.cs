using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Services;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests
{
    public class ImageUploadingTests
    {
        private readonly ImageUploadService _imageUploadService;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly IImageIdentifierProvider _imageIdentifierProvider;
        public ImageUploadingTests(ITestOutputHelper output)
        {
            _blobStorage = new MockImageBlobStorage();
            _metadataStorage = new MockImageMetadataStorage();
            
            _imageIdentifierProvider = new ImageIdentifierProvider();
            IImageMetadataReader imageMetadataReader = new ImageMetadataReader();
            IImageValidator imageValidator = new MockImageValidator();

            _imageUploadService = new ImageUploadService(output.BuildLoggerFor<ImageUploadService>(), imageMetadataReader, _blobStorage,
                _metadataStorage, imageValidator, _imageIdentifierProvider);
        }

        [Fact]
        public async Task ImageMetadataShouldBeSaved()
        {
            string partitionKey = Guid.NewGuid().ToString();
            var uploadedImage = await UploadImage(partitionKey);
            
            var metadataResult = await _metadataStorage.GetMetadata(new List<string> { uploadedImage.Id }, partitionKey, CancellationToken.None);
            Assert.True(metadataResult.Any());
            var metadata = metadataResult[0];
            Assert.Equal(uploadedImage.Bytes, metadata.SizeBytes);
            Assert.Equal(uploadedImage.Hash, metadata.MD5Hash);
            Assert.Equal(uploadedImage.Height, metadata.Height);
            Assert.Equal(uploadedImage.Width, metadata.Width);
            Assert.Equal(uploadedImage.DateAdded, metadata.DateAddedUtc);
        }
        
        [Fact]
        public async Task ImageBinaryShouldBeSaved()
        {
            string partitionKey = Guid.NewGuid().ToString();
            var uploadedImage = await UploadImage(partitionKey);

            var imageName = _imageIdentifierProvider.GetImageName(uploadedImage.Id, "jpg");
            var stream = await _blobStorage.DownloadImage(imageName, ImageSizeType.Original, CancellationToken.None);
            Assert.True(stream.Length > 0);
        }

        private async Task<ImageInfo> UploadImage(string partitionKey)
        {
            await using FileStream file = ImageTestsHelper.OpenFileForReading("Images\\jpeg\\1043-800x600.jpg");
            return await _imageUploadService.UploadImage(file, partitionKey);
        }
    }
}