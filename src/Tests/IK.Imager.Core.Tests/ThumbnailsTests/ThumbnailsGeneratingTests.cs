using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Settings;
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
        private readonly Mock<IImageResizing> _imageResizingMock;
        private readonly Mock<IOptions<ImageThumbnailsSettings>> _imageThumbnailSettingsMock;
        private readonly ILogger<CreateThumbnailsCommandHandler> _logger;
        private readonly IImageIdentifierProvider _imageIdentifierProvider; 
        
        public ThumbnailsGeneratingTests(ITestOutputHelper output)
        {
            _imageResizingMock = new Mock<IImageResizing>();
            _blobRepositoryMock = new Mock<IImageBlobRepository>();
            _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
            _imageThumbnailSettingsMock = new Mock<IOptions<ImageThumbnailsSettings>>();
            _logger = output.BuildLoggerFor<CreateThumbnailsCommandHandler>(); 
            _imageIdentifierProvider = new ImageIdentifierProvider();
        }

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
            
            var thumbnailsCommandHandler = new CreateThumbnailsCommandHandler(_logger, _imageResizingMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageIdentifierProvider, _imageThumbnailSettingsMock.Object);

            await thumbnailsCommandHandler.Handle(new Fixture().Create<CreateThumbnailsCommand>(),
                CancellationToken.None);
            
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
            
            var thumbnailsCommandHandler = new CreateThumbnailsCommandHandler(_logger, _imageResizingMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageIdentifierProvider, _imageThumbnailSettingsMock.Object);

            await thumbnailsCommandHandler.Handle(new Fixture().Create<CreateThumbnailsCommand>(),
                CancellationToken.None);
            
            //verifying that image download is not called
            _blobRepositoryMock.Verify(x => x.DownloadImage(
                It.IsAny<string>(), 
                ImageSizeType.Original, 
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateThumbnails_Bmp_PngThumbnailsGenerated()
        {
            ImageMetadata imageMetadata = new Fixture().Create<ImageMetadata>();
            imageMetadata.Width = 500;

            _imageThumbnailSettingsMock.Setup(x => x.Value)
                .Returns(new ImageThumbnailsSettings { TargetWidth = new[]
                {
                    imageMetadata.Width - 100,
                    imageMetadata.Width - 200
                } });
            
            await MockForPositiveFlow(imageMetadata);

            _imageResizingMock.Verify(x => x.Resize(It.IsAny<Stream>(),
                IK.Imager.Core.Abstractions.Models.ImageType.PNG, It.IsAny<int>()), Times.AtLeastOnce);
            
            _metadataRepositoryMock.Verify(x => x.SetMetadata(It.IsAny<ImageMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task CreateThumbnails_ProperThumbnailsGenerated()
        {
            ImageMetadata imageMetadata = new Fixture().Create<ImageMetadata>();
            imageMetadata.Width = 2000;
            imageMetadata.ImageType = ImageType.PNG;

            _imageThumbnailSettingsMock.Setup(x => x.Value)
                .Returns(new ImageThumbnailsSettings { TargetWidth = new[] { 2200, 1600, 900, 500 }});
            
            await MockForPositiveFlow(imageMetadata);

            _imageResizingMock.Verify(x => x.Resize(It.IsAny<Stream>(),
                IK.Imager.Core.Abstractions.Models.ImageType.PNG, It.IsAny<int>()), Times.Exactly(3));

            _metadataRepositoryMock.Verify(x => x.SetMetadata(It.Is<ImageMetadata>(i =>
                    i.Thumbnails.Count == 3), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        private async Task MockForPositiveFlow(ImageMetadata imageMetadata)
        {
            _metadataRepositoryMock.Setup(x => x.GetMetadata(
                    It.IsAny<ICollection<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ImageMetadata> { imageMetadata });

            _blobRepositoryMock.Setup(x => x.DownloadImage(
                    It.IsAny<string>(),
                    It.IsAny<ImageSizeType>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream());
            
            _blobRepositoryMock.Setup(x => x.UploadImage(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<ImageSizeType>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Fixture().Create<UploadImageResult>());

            _imageResizingMock.Setup(x => x.Resize(
                    It.IsAny<Stream>(),
                    It.IsAny<IK.Imager.Core.Abstractions.Models.ImageType>(),
                    It.IsAny<int>()))
                .Returns(new ImageResizingResult()
                {
                    Image = new MemoryStream(),
                    Size = new Fixture().Create<ImageSize>()
                });
            
            var thumbnailsCommandHandler = new CreateThumbnailsCommandHandler(_logger, _imageResizingMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageIdentifierProvider, _imageThumbnailSettingsMock.Object);

            await thumbnailsCommandHandler.Handle(new Fixture().Create<CreateThumbnailsCommand>(),
                CancellationToken.None);
        }
    }
}