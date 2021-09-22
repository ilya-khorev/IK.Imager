using System;
using System.Linq;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using ImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

namespace IK.Imager.Core.Tests
{
    public class ThumbnailsGeneratingTests
    {
        private readonly ImageThumbnailService _thumbnailsService;
        private readonly IImageBlobRepository _blobRepository;
        private readonly IImageMetadataRepository _metadataRepository;
        private readonly IOptions<ImageThumbnailsSettings> _imageThumbnailSettings;
        
        public ThumbnailsGeneratingTests(ITestOutputHelper output)
        {
            IImageResizing imageResizing = new ImageResizing();

            _blobRepository = new InMemoryMockedImageBlobRepository();
            _metadataRepository = new InMemoryMockedImageMetadataRepository();

            _imageThumbnailSettings = new MockImageThumbnailsSettings();
            
            IImageIdentifierProvider imageIdentifierProvider = new ImageIdentifierProvider();
            _thumbnailsService = new ImageThumbnailService(output.BuildLoggerFor<ImageThumbnailService>(), imageResizing, _blobRepository, _metadataRepository, imageIdentifierProvider, _imageThumbnailSettings);
        }
        
        [Fact]
        public async Task ShouldGenerateThumbnails()
        {
            string imageGroup = Guid.NewGuid().ToString();
            string contentType = "image/jpeg";
            int width = 800;
            int height = 600;
            double aspectRatio = width / (double)height; 
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobRepository, _metadataRepository,"Images\\jpeg\\1043-800x600.jpg", width, height, contentType, ImageType.JPEG, imageGroup);

            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, imageGroup);
            Assert.Equal(_imageThumbnailSettings.Value.TargetWidth.Length, imageWithGeneratedThumbnails.Count);
            int i = 0;
            foreach (var imageThumbnail in imageWithGeneratedThumbnails)
            {
                Assert.Equal(_imageThumbnailSettings.Value.TargetWidth[i++],imageThumbnail.Width);
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
            string imageGroup = Guid.NewGuid().ToString();
            string contentType = "image/gif";
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobRepository, _metadataRepository, "Images\\gif\\giphy_200x200.gif", 200, 200, contentType, ImageType.GIF, imageGroup);
            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, imageGroup);
            Assert.False(imageWithGeneratedThumbnails.Any());
        }

        [Fact]
        public async Task ShouldGeneratePngThumbnailsForBmpImage()
        {
            string imageGroup = Guid.NewGuid().ToString();
            string contentType = "image/bmp";
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobRepository, _metadataRepository,"Images\\bmp\\1068-800x1600.bmp", 800, 1200, contentType, ImageType.BMP, imageGroup);
            var imageWithGeneratedThumbnails = await _thumbnailsService.GenerateThumbnails(uploadImageResult.Id, imageGroup);

            foreach (var imageThumbnail in imageWithGeneratedThumbnails)
            {
                Assert.Equal("image/png", imageThumbnail.MimeType);
            }
        }
    }
}