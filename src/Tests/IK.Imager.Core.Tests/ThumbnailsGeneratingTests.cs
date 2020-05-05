using System;
using System.Linq;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Configuration;
using IK.Imager.Core.Services;
using IK.Imager.Core.Tests.Mocks;
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
            int width = 800;
            int height = 600;
            double aspectRatio = width / (double)height; 
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobStorage, _metadataStorage,"Images\\jpeg\\1043-800x600.jpg", width, height, contentType, ImageType.JPEG, partitionKey);

            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, partitionKey);
            Assert.Equal(Value.TargetWidth.Length, imageWithGeneratedThumbnails.Count);
            int i = 0;
            foreach (var imageThumbnail in imageWithGeneratedThumbnails)
            {
                Assert.Equal(Value.TargetWidth[i++],imageThumbnail.Width);
                Assert.Equal(contentType,imageThumbnail.MimeType);
                Assert.NotNull(imageThumbnail.Id);
                
                //Making sure aspect ration is retained
                double thumbnailAspectRation = imageThumbnail.Width / (double)imageThumbnail.Height;
                Assert.Equal(aspectRatio, thumbnailAspectRation);
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
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobStorage, _metadataStorage, "Images\\gif\\giphy_200x200.gif", 200, 200, contentType, ImageType.GIF, partitionKey);
            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, partitionKey);
            Assert.False(imageWithGeneratedThumbnails.Any());
        }

        [Fact]
        public async Task ShouldGeneratePngThumbnailsForBmpImage()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string contentType = "image/bmp";
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobStorage, _metadataStorage,"Images\\bmp\\1068-800x1600.bmp", 800, 1200, contentType, ImageType.BMP, partitionKey);
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
    }
}