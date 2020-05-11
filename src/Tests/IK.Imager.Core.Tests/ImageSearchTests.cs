﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Services;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Storage.Abstractions.Storage;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests
{
    public class ImageSearchTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        private readonly ImageSearchService _imageSearchService;
        private readonly ImageUploadService _imageUploadService;

        public ImageSearchTests(ITestOutputHelper output)
        {
            _output = output;
            _blobStorage = new MockImageBlobStorage();
            _metadataStorage = new MockImageMetadataStorage();
            _imageSearchService = new ImageSearchService(output.BuildLoggerFor<ImageSearchService>(), _metadataStorage,
                _blobStorage, new MockCdnService());

            var imageIdentifierProvider = new ImageIdentifierProvider();
            IImageMetadataReader imageMetadataReader = new ImageMetadataReader();
            IImageValidator imageValidator = new MockImageValidator();
            _imageUploadService = new ImageUploadService(output.BuildLoggerFor<ImageUploadService>(),
                imageMetadataReader, _blobStorage,
                _metadataStorage, imageValidator, imageIdentifierProvider);
        }

        [Fact]
        public async Task ShouldFindImageById()
        {
            var partitionKey = Guid.NewGuid().ToString();
            var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(partitionKey, _imageUploadService);
            var imagesSearchResult = await _imageSearchService.Search(new[] {uploadedImage.Id}, partitionKey);

            Assert.Single(imagesSearchResult.Images);
            Assert.Equal(uploadedImage.Id, imagesSearchResult.Images[0].Id);
        }
        
        [Fact]
        public async Task ShouldFindMultiplesImagesById()
        {
            var partitionKey = Guid.NewGuid().ToString();

            List<string> ids = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(partitionKey, _imageUploadService);
                ids.Add(uploadedImage.Id);
            }
            
            var imagesSearchResult = await _imageSearchService.Search(ids.ToArray(), partitionKey);
            Assert.Equal(ids.Count, imagesSearchResult.Images.Count);
        }

        [Fact]
        public async Task ImageModelShouldHaveAllFieldsCompleted()
        {
            var partitionKey = Guid.NewGuid().ToString();
            var uploadedImage = await ImageTestsHelper.UploadRandomlySelectedImage(partitionKey, _imageUploadService);
            var imagesSearchResult = await _imageSearchService.Search(new[] {uploadedImage.Id}, partitionKey);
            var resultImage = imagesSearchResult.Images[0];

            var imageMetadata = (await _metadataStorage.GetMetadata(new[] {uploadedImage.Id}, partitionKey, CancellationToken.None))[0];
            Assert.Equal(imageMetadata.SizeBytes, resultImage.Bytes);
            Assert.Equal(imageMetadata.MD5Hash, resultImage.Hash);
            Assert.Equal(imageMetadata.Height, resultImage.Height);
            Assert.Equal(imageMetadata.Width, resultImage.Width);
            Assert.Empty(resultImage.Tags);
            Assert.Equal(imageMetadata.DateAddedUtc, resultImage.DateAdded);
            Assert.Equal(imageMetadata.MimeType, resultImage.MimeType);
            Assert.NotEmpty(resultImage.Url);
            Assert.False(resultImage.Thumbnails.Any());
            
            var imageThumbnailSettings = new MockImageThumbnailsSettings();
            IImageResizing imageResizing = new ImageResizing();
            IImageIdentifierProvider imageIdentifierProvider = new ImageIdentifierProvider();
            var thumbnailsService = new ImageThumbnailService(_output.BuildLoggerFor<ImageThumbnailService>(), imageResizing, _blobStorage, 
                _metadataStorage, imageIdentifierProvider, imageThumbnailSettings);
            await thumbnailsService.GenerateThumbnails(uploadedImage.Id, partitionKey);
            
            imagesSearchResult = await _imageSearchService.Search(new[] {uploadedImage.Id}, partitionKey);
            imageMetadata = (await _metadataStorage.GetMetadata(new[] {uploadedImage.Id}, partitionKey, CancellationToken.None))[0];
            resultImage = imagesSearchResult.Images[0];
            Assert.True(resultImage.Thumbnails.Any());
            foreach (var imageThumbnail in resultImage.Thumbnails)
            {
                var matchedThumbnail = imageMetadata.Thumbnails.Single(x => x.Id == imageThumbnail.Id);
                Assert.Equal(matchedThumbnail.SizeBytes, imageThumbnail.Bytes);
                Assert.Equal(matchedThumbnail.MD5Hash, imageThumbnail.Hash);
                Assert.Equal(matchedThumbnail.Height, imageThumbnail.Height);
                Assert.Equal(matchedThumbnail.Width, imageThumbnail.Width);
                Assert.Equal(matchedThumbnail.DateAddedUtc, imageThumbnail.DateAdded);
                Assert.Equal(matchedThumbnail.MimeType, imageThumbnail.MimeType);
                Assert.NotEmpty(imageThumbnail.Url);
            }
        }
    }
}