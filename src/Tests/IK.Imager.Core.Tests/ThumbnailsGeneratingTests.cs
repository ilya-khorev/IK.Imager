using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Configuration;
using IK.Imager.Core.Services;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using ImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

namespace IK.Imager.Core.Tests
{
    public class ThumbnailsGeneratingTests: IOptions<ImageThumbnailsSettings>
    {
        private readonly ImageThumbnailService _thumbnailsService;
        private readonly IImageBlobStorage _blobStorage;
        private readonly IImageMetadataStorage _metadataStorage;
        
        public ThumbnailsGeneratingTests(ITestOutputHelper output)
        {
            IImageResizing imageResizing = new ImageResizing();

            _blobStorage = new MockImageBlobStorage();
            _metadataStorage = new MockImageMetadataStorage();
            
            IImageIdentifierProvider imageIdentifierProvider = new ImageIdentifierProvider();
            _thumbnailsService = new ImageThumbnailService(output.BuildLoggerFor<ImageThumbnailService>(), imageResizing, _blobStorage, _metadataStorage, imageIdentifierProvider, this);
        }
        
        [Fact]
        public async Task ShouldGenerateThumbnails()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string contentType = "image/jpeg";
            var uploadImageResult = await UploadImage("Images\\jpeg\\1043-800x600.jpg", 800, 600, contentType, ImageType.JPEG, partitionKey);

            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, partitionKey);
            Assert.Equal(Value.TargetWidth.Length, imageWithGeneratedThumbnails.Count);
            int i = 0;
            foreach (var imageThumbnail in imageWithGeneratedThumbnails)
            {
                Assert.Equal(Value.TargetWidth[i++],imageThumbnail.Width);
                Assert.Equal(contentType,imageThumbnail.MimeType);
                Assert.NotNull(imageThumbnail.Id);
                Assert.True(imageThumbnail.Height > 0);
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
            var uploadImageResult = await UploadImage("Images\\gif\\giphy_200x200.gif", 200, 200, contentType, ImageType.GIF, partitionKey);
            Assert.Null(uploadImageResult.Thumbnails);
        }

        [Fact]
        public async Task ShouldGeneratePngThumbnailsForBmpImage()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string contentType = "image/bmp";
            var uploadImageResult = await UploadImage("Images\\bmp\\1068-800x1600.bmp", 800, 1200, contentType, ImageType.BMP, partitionKey);
            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, partitionKey);

            foreach (var imageThumbnail in imageWithGeneratedThumbnails)
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
            string extension = "";
            switch (imageType)
            {
                case ImageType.BMP:
                    extension = ".bmp";
                    break;
                
                case ImageType.JPEG:
                    extension = ".jpeg";
                    break;
                
                case ImageType.PNG:
                    extension = ".png";
                    break;
                
                case ImageType.GIF:
                    extension = ".gif";
                    break;
            }
            
            await using FileStream file = OpenFileForReading(imagePath);
            string imageId = Guid.NewGuid().ToString();
            string imageName = imageId + extension;
            var uploadImageResult = await _blobStorage.UploadImage(imageName, file, ImageSizeType.Original, contentType, CancellationToken.None);

            var imageMetadata = new ImageMetadata
            {
                Id = imageId,
                Name = imageName,
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