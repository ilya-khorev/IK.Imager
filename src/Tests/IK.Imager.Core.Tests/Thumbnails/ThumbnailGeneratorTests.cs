using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.Thumbnails
{
    public class ThumbnailGeneratorTests
    {
        private readonly Mock<IImageBlobRepository> _blobRepositoryMock;
        private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
        private readonly Mock<IImageResizer> _imageResizerMock;
        private readonly Mock<IOptions<ImageThumbnailsSettings>> _imageThumbnailSettingsMock;
        private readonly ILogger<ThumbnailGenerator> _logger;
        private readonly IImageNameGenerator _imageNameGenerator;

        public ThumbnailGeneratorTests(ITestOutputHelper output)
        {
            _imageResizerMock = new Mock<IImageResizer>();
            _blobRepositoryMock = new Mock<IImageBlobRepository>();
            _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
            _imageThumbnailSettingsMock = new Mock<IOptions<ImageThumbnailsSettings>>();
            _logger = output.BuildLoggerFor<ThumbnailGenerator>();
            _imageNameGenerator = new ImageNameGenerator();
        }

        [Fact]
        public async Task Generate_ImageMetadataNotFound_SkipsBlobDownload()
        {
            _imageThumbnailSettingsMock.Setup(x => x.Value)
                .Returns(new ImageThumbnailsSettings { TargetWidth = new[] { 500, 1000 } });

            //setting up so that no image metadata is returned
            _metadataRepositoryMock.Setup(x => x.GetMetadata(
                It.IsAny<ICollection<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ImageMetadata>());

            var thumbnailGenerator = new ThumbnailGenerator(_logger, _imageResizerMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageNameGenerator, _imageThumbnailSettingsMock.Object);

            await thumbnailGenerator.Generate(new Fixture().Create<string>(), new Fixture().Create<string>(),
                CancellationToken.None);

            //verifying that image download is not called
            _blobRepositoryMock.Verify(x => x.DownloadImage(
                It.IsAny<string>(),
                ImageVariant.Original,
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Generate_NarrowerThanSmallestTargetWidth_SkipsBlobDownload()
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

            var thumbnailGenerator = new ThumbnailGenerator(_logger, _imageResizerMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageNameGenerator, _imageThumbnailSettingsMock.Object);

            await thumbnailGenerator.Generate(new Fixture().Create<string>(), new Fixture().Create<string>(),
                CancellationToken.None);

            //verifying that image download is not called
            _blobRepositoryMock.Verify(x => x.DownloadImage(
                It.IsAny<string>(),
                ImageVariant.Original,
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Generate_BmpOriginal_GeneratesPngThumbnails()
        {
            ImageMetadata imageMetadata = new Fixture().Create<ImageMetadata>();
            imageMetadata.Width = 500;

            _imageThumbnailSettingsMock.Setup(x => x.Value)
                .Returns(new ImageThumbnailsSettings
                {
                    TargetWidth = new[]
                {
                    imageMetadata.Width - 100,
                    imageMetadata.Width - 200
                }
                });

            await MockForPositiveFlow(imageMetadata);

            _imageResizerMock.Verify(x => x.Resize(It.IsAny<Stream>(),
                ImageType.PNG, It.IsAny<int>()), Times.AtLeastOnce);

            _metadataRepositoryMock.Verify(x => x.UpdateMetadata(It.IsAny<ImageMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Generate_WiderThanEveryTargetWidth_GeneratesThumbnails()
        {
            ImageMetadata imageMetadata = new Fixture().Create<ImageMetadata>();
            imageMetadata.Width = 2000;
            imageMetadata.ImageType = ImageType.PNG;

            _imageThumbnailSettingsMock.Setup(x => x.Value)
                .Returns(new ImageThumbnailsSettings { TargetWidth = new[] { 2200, 1600, 900, 500 } });

            await MockForPositiveFlow(imageMetadata);

            _imageResizerMock.Verify(x => x.Resize(It.IsAny<Stream>(),
                ImageType.PNG, It.IsAny<int>()), Times.Exactly(3));

            _metadataRepositoryMock.Verify(x => x.UpdateMetadata(It.Is<ImageMetadata>(i =>
                    i.Thumbnails!.Count == 3), It.IsAny<CancellationToken>()), Times.Once);
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
                    It.IsAny<ImageVariant>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream());

            _blobRepositoryMock.Setup(x => x.UploadImage(
                    It.IsAny<string>(),
                    It.IsAny<Stream>(),
                    It.IsAny<ImageVariant>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Fixture().Create<BlobUploadResult>());

            _imageResizerMock.Setup(x => x.Resize(
                    It.IsAny<Stream>(),
                    It.IsAny<ImageType>(),
                    It.IsAny<int>()))
                .Returns(new ImageResizeResult(new MemoryStream(), new Fixture().Create<ImageSize>()));

            var thumbnailGenerator = new ThumbnailGenerator(_logger, _imageResizerMock.Object,
                _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
                _imageNameGenerator, _imageThumbnailSettingsMock.Object);

            await thumbnailGenerator.Generate(new Fixture().Create<string>(), new Fixture().Create<string>(),
                CancellationToken.None);
        }
    }
}
