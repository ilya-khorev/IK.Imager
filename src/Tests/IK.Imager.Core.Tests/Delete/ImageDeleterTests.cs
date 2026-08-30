using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Delete;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.Delete;

public class ImageDeleterTests
{
    private readonly ILogger<ImageDeleter> _logger;
    private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock;
    private readonly Mock<IImageEvents> _imageEventsMock;

    public ImageDeleterTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<ImageDeleter>();
        _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
        _blobRepositoryMock = new Mock<IImageBlobRepository>();
        _imageEventsMock = new Mock<IImageEvents>();
    }

    private ImageDeleter CreateImageDeleter() =>
        new(_logger, _metadataRepositoryMock.Object, _blobRepositoryMock.Object, _imageEventsMock.Object);

    [Fact]
    public async Task DeleteMetadata_ImageExists_ReturnsTrueAndRaisesEvent()
    {
        _metadataRepositoryMock.Setup(x => x.GetMetadata(
                It.IsAny<ICollection<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImageMetadata> { new Fixture().Create<ImageMetadata>() });

        _metadataRepositoryMock.Setup(x => x.RemoveMetadata(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateImageDeleter().DeleteMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
            CancellationToken.None);
        Assert.True(result);

        _imageEventsMock.Verify(x => x.ImageMetadataDeleted(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string[]>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteMetadata_ImageDoesNotExist_ReturnsFalse()
    {
        _metadataRepositoryMock.Setup(x => x.GetMetadata(
                It.IsAny<ICollection<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImageMetadata>());

        var result = await CreateImageDeleter().DeleteMetadata(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
            CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFiles_ImageWithThumbnails_DeletesOriginalAndThumbnails()
    {
        _blobRepositoryMock.Setup(x => x.TryDeleteImage(It.IsAny<string>(),
            It.IsAny<ImageVariant>(), CancellationToken.None));

        var imageId = Guid.NewGuid().ToString();
        var blobPath = Guid.NewGuid().ToString();
        var thumbnailBlobPaths = new Fixture().CreateMany<string>(3).ToArray();

        await CreateImageDeleter().DeleteFiles(imageId, blobPath, thumbnailBlobPaths, CancellationToken.None);

        _blobRepositoryMock.Verify(x => x.TryDeleteImage(blobPath, ImageVariant.Original, CancellationToken.None), Times.Once);
        foreach (var thumbnailBlobPath in thumbnailBlobPaths)
        {
            _blobRepositoryMock.Verify(x => x.TryDeleteImage(thumbnailBlobPath, ImageVariant.Thumbnail, CancellationToken.None), Times.Once);
        }
    }

    [Fact]
    public async Task DeleteFiles_ImageWithoutThumbnails_DeletesOriginalOnly()
    {
        _blobRepositoryMock.Setup(x => x.TryDeleteImage(It.IsAny<string>(),
            It.IsAny<ImageVariant>(), CancellationToken.None));

        var imageId = Guid.NewGuid().ToString();
        var blobPath = Guid.NewGuid().ToString();

        await CreateImageDeleter().DeleteFiles(imageId, blobPath, [], CancellationToken.None);

        _blobRepositoryMock.Verify(x => x.TryDeleteImage(blobPath, ImageVariant.Original, CancellationToken.None), Times.Once);
        _blobRepositoryMock.Verify(x => x.TryDeleteImage(It.IsAny<string>(), ImageVariant.Thumbnail, CancellationToken.None), Times.Never);
    }
}
