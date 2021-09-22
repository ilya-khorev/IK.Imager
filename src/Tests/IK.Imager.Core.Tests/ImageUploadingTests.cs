﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.ImagesCrud;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests
{
    public class ImageUploadingTests
    {
        private readonly ImageUploadService _imageUploadService;
        private readonly IImageBlobRepository _blobRepository;
        private readonly IImageMetadataRepository _metadataRepository;
        public ImageUploadingTests(ITestOutputHelper output)
        {
            _blobRepository = new InMemoryMockedImageBlobRepository();
            _metadataRepository = new InMemoryMockedImageMetadataRepository();
            
            var imageIdentifierProvider = new ImageIdentifierProvider();
            IImageMetadataReader imageMetadataReader = new ImageMetadataReader();
            IImageValidator imageValidator = new MockImageValidator();

            _imageUploadService = new ImageUploadService(output.BuildLoggerFor<ImageUploadService>(), imageMetadataReader, _blobRepository,
                _metadataRepository, imageValidator, imageIdentifierProvider, new MockCdnService());
        }

        [Fact]
        public async Task ImageMetadataShouldBeSaved()
        {
            string imageGroup = Guid.NewGuid().ToString();
            var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(imageGroup, _imageUploadService);
            
            var metadataResult = await _metadataRepository.GetMetadata(new List<string> { uploadedImage.Id }, imageGroup, CancellationToken.None);
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
            string imageGroup = Guid.NewGuid().ToString();
            var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(imageGroup, _imageUploadService);
            var stream = await _blobRepository.DownloadImage(uploadedImage.Name, ImageSizeType.Original, CancellationToken.None);
            Assert.True(stream.Length > 0);
        }
    }
}