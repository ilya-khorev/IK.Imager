using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.BackgroundService.Configuration;
using IK.Imager.BackgroundService.Services;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.BackgroundServiceTests
{
    public class ThumbnailsGeneratingTests: IOptions<ImageThumbnailsSettings>
    {
        private readonly ThumbnailsService _thumbnailsService;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        
        public ThumbnailsGeneratingTests(ITestOutputHelper output)
        {
            IImageResizing imageResizing = new ImageResizing();
            
            _blobStorage = new ImageBlobAzureStorage(new ImageAzureStorageConfiguration
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ImagesContainerName = "images",
                ThumbnailsContainerName = "thumbnails"
            });
            
            _metadataStorage = new ImageMetadataCosmosDbStorage(new ImageMetadataCosmosDbStorageConfiguration
            {
                ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                ContainerId = "ImageMetadataContainer",
                ContainerThroughPutOnCreation = 1000,
                DatabaseId = "ImageMetadataDb"
            });
            
            _thumbnailsService = new ThumbnailsService(output.BuildLoggerFor<ThumbnailsService>(), imageResizing, _blobStorage, _metadataStorage, this);
        }
        
        [Fact]
        public async Task ShouldGenerateThumbnails()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string contentType = "image/jpeg";
            var uploadImageResult = await UploadImage("Images\\1043-800x600.jpg", 800, 600, contentType, partitionKey);

            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, partitionKey);
            Assert.Equal(Value.TargetWidth.Length, imageWithGeneratedThumbnails.Thumbnails.Count);
            int i = 0;
            foreach (var imageThumbnail in imageWithGeneratedThumbnails.Thumbnails)
            {
                Assert.Equal(Value.TargetWidth[i++],imageThumbnail.Width);
                Assert.Equal(contentType,imageThumbnail.MimeType);

                Assert.NotNull(imageThumbnail.Id);
                Assert.NotNull(imageThumbnail.MD5Hash);
                Assert.True(imageThumbnail.Height > 0);
                Assert.True(imageThumbnail.SizeBytes > 0);
            }
        }

        [Fact]
        public void ShouldGenerateNothingWhenNotFound()
        {
        }

        [Fact]
        public void ShouldGenerateNothingWhenImageIsSmall()
        {
        }

        [Fact]
        public void ShouldGeneratePngThumbnailsForBmpImage()
        {
        }

        public ImageThumbnailsSettings Value { get; } = new ImageThumbnailsSettings
        {
            TargetWidth = new [] { 200, 300, 500 }
        };

        private async Task<ImageMetadata> UploadImage(string imagePath, int width, int height, string contentType, string partitionKey)
        {
            await using FileStream file = OpenFileForReading(imagePath);
            var uploadImageResult = await _blobStorage.UploadImage(file, ImageSizeType.Original, contentType, CancellationToken.None);

            var imageMetadata = new ImageMetadata
            {
                Id = uploadImageResult.Id,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = height,
                Width = width,
                MD5Hash = uploadImageResult.MD5Hash,
                SizeBytes = file.Length,
                MimeType = contentType,
                PartitionKey = partitionKey 
            };

            await _metadataStorage.SetMetadata(imageMetadata, CancellationToken.None);
            return imageMetadata;
        }
        
        private FileStream OpenFileForReading(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read);
        }
    }
}