using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Tests.Mocks;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;
using ImageType = IK.Imager.Storage.Abstractions.Models.ImageType;

namespace IK.Imager.Core.Tests.ThumbnailsTests
{
    public class ThumbnailsGeneratingTests
    {
        private readonly Mock<IImageBlobRepository> _blobRepositoryMock;
        private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
        private readonly Mock<ImageResizing> _imageResizingMock;
        private readonly Mock<IOptions<ImageThumbnailsSettings>> _imageThumbnailSettingsMock;
        private readonly ILogger<ImageThumbnailService> _logger;
        private readonly IImageIdentifierProvider _imageIdentifierProvider; 
        
        public ThumbnailsGeneratingTests(ITestOutputHelper output)
        {
            _imageResizingMock = new Mock<ImageResizing>();
            _blobRepositoryMock = new Mock<IImageBlobRepository>();
            _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
            _imageThumbnailSettingsMock = new Mock<IOptions<ImageThumbnailsSettings>>();
            _logger = output.BuildLoggerFor<ImageThumbnailService>(); 
            _imageIdentifierProvider = new ImageIdentifierProvider();
        }
        
        /*
        [Fact]
        public async Task ShouldGenerateThumbnails()
        {
            string imageGroup = Guid.NewGuid().ToString();
            string contentType = "image/jpeg";
            int width = 800;
            int height = 600;
            double aspectRatio = width / (double)height; 
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobRepository, _metadataRepository,"Images\\jpeg\\1043-800x600.jpg", width, height, contentType, ImageType.JPEG, imageGroup);

            var imageWithGeneratedThumbnails = await _thumbnailsService.CreateThumbnails(uploadImageResult.Id, imageGroup);
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
        }*/

        [Fact]
        public async Task CreateThumbnails_ImageMetadataNotFound_SkippedBlobDownloading()
        {
            _imageThumbnailSettingsMock.Setup(x => x.Value)
                .Returns(new ImageThumbnailsSettings { TargetWidth = new[] { 500, 1000 } });
            
            //setting up so that no image metadata is returned
            _metadataRepositoryMock.Setup(x => x.GetMetadata(
                It.IsAny<ICollection<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ImageMetadata>());
            
            var thumbnailsService = new ImageThumbnailService(_logger, _imageResizingMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageIdentifierProvider, _imageThumbnailSettingsMock.Object);

            await thumbnailsService.CreateThumbnails(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            
            //verifying that image download is not called
            _blobRepositoryMock.Verify(x => x.DownloadImage(
                It.IsAny<string>(), 
                ImageSizeType.Original, 
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateThumbnails_TargetWidthIsGreaterThanOriginalImageWidth_SkippedBlobDownloading()
        {
            ImageMetadata imageMetadata = new Fixture().Create<ImageMetadata>();
            imageMetadata.Width = 500;
            imageMetadata.Height = 500;
            
            //set the min target width to 600, so that it would not need to create any thumbnails
            _imageThumbnailSettingsMock.Setup(x => x.Value)
                .Returns(new ImageThumbnailsSettings { TargetWidth = new[] { imageMetadata.Width + 100 } });
            
            //setting up so that imageMetadata defined above is returned
            _metadataRepositoryMock.Setup(x => x.GetMetadata(
                    It.IsAny<ICollection<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ImageMetadata> { imageMetadata });
            
            var thumbnailsService = new ImageThumbnailService(_logger, _imageResizingMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageIdentifierProvider, _imageThumbnailSettingsMock.Object);

            await thumbnailsService.CreateThumbnails(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            
            //verifying that image download is not called
            _blobRepositoryMock.Verify(x => x.DownloadImage(
                It.IsAny<string>(), 
                ImageSizeType.Original, 
                It.IsAny<CancellationToken>()), Times.Never);
        }

        /*
        [Fact]
        public async Task ShouldGeneratePngThumbnailsForBmpImage()
        {
            string imageGroup = Guid.NewGuid().ToString();
            string contentType = "image/bmp";
            var uploadImageResult = await ImageTestsHelper.UploadImage(_blobRepository, _metadataRepository,"Images\\bmp\\1068-800x1600.bmp", 800, 1200, contentType, ImageType.BMP, imageGroup);
            var imageWithGeneratedThumbnails = await _thumbnailsService.CreateThumbnails(uploadImageResult.Id, imageGroup);

            foreach (var imageThumbnail in imageWithGeneratedThumbnails)
            {
                Assert.Equal("image/png", imageThumbnail.MimeType);
            }
        }*/
    }
}