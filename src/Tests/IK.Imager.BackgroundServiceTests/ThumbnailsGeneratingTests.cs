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
using IK.Imager.TestsBase;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using ImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

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
                ConnectionString = Constants.AzureConnectionString,
                ImagesContainerName = Constants.ImagesContainerName,
                ThumbnailsContainerName = Constants.ThumbnailsContainerName
            });
            
            _metadataStorage = new ImageMetadataCosmosDbStorage(new ImageMetadataCosmosDbStorageConfiguration
            {
                ConnectionString = Constants.CosmosDbConnectionString,
                ContainerId = Constants.ContainerId,
                ContainerThroughPutOnCreation = Constants.ContainerThroughPutOnCreation,
                DatabaseId = Constants.DatabaseId
            });
            
            IImageIdentifierProvider imageIdentifierProvider = new ImageIdentifierProvider();
            
            _thumbnailsService = new ThumbnailsService(output.BuildLoggerFor<ThumbnailsService>(), imageResizing, _blobStorage, _metadataStorage, imageIdentifierProvider, this);
        }
        
        [Fact]
        public async Task ShouldGenerateThumbnails()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string contentType = "image/jpeg";
            var uploadImageResult = await UploadImage("Images\\1043-800x600.jpg", 800, 600, contentType, ImageType.JPEG, partitionKey);

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
        public async Task ShouldGenerateNothingWhenNotFound()
        {
            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Assert.Null(imageWithGeneratedThumbnails);
        }

        [Fact]
        public async Task ShouldGenerateNothingWhenImageIsSmall()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string contentType = "image/gif";
            var uploadImageResult = await UploadImage("Images\\giphy_200x200.gif", 200, 200, contentType, ImageType.GIF, partitionKey);
            Assert.Null(uploadImageResult.Thumbnails);
        }

        [Fact]
        public async Task ShouldGeneratePngThumbnailsForBmpImage()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string contentType = "image/bmp";
            var uploadImageResult = await UploadImage("Images\\1068-800x1600.bmp", 800, 1200, contentType, ImageType.BMP, partitionKey);
            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, partitionKey);

            foreach (var imageThumbnail in imageWithGeneratedThumbnails.Thumbnails)
            {
                Assert.Equal("image/png", imageThumbnail.MimeType);
            }
        }

        public ImageThumbnailsSettings Value { get; } = new ImageThumbnailsSettings
        {
            TargetWidth = new [] { 200, 300, 500 }
        };

        private async Task<ImageMetadata> UploadImage(string imagePath, int width, int height, string contentType, ImageType imageType, string partitionKey)
        {
            await using FileStream file = OpenFileForReading(imagePath);
            string imageId = Guid.NewGuid().ToString();
            var uploadImageResult = await _blobStorage.UploadImage(imageId, file, ImageSizeType.Original, contentType, CancellationToken.None);

            var imageMetadata = new ImageMetadata
            {
                Id = imageId,
                DateAddedUtc = uploadImageResult.DateAdded.DateTime,
                Height = height,
                Width = width,
                MD5Hash = uploadImageResult.MD5Hash,
                SizeBytes = file.Length,
                MimeType = contentType,
                ImageType = imageType,
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