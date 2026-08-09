using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Deleting;
using IK.Imager.Core.Deleting;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.DeletionTests;

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
    public async Task DeleteMetadata_ImageExists_ShouldBeDeleted()
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
    public async Task DeleteFiles_ImageWithThumbnails_ShouldBeDeleted()
    {
        _blobRepositoryMock.Setup(x => x.TryDeleteImage(It.IsAny<string>(),
            It.IsAny<ImageSizeType>(), CancellationToken.None));

        var imageId = Guid.NewGuid().ToString();
        var imageName = Guid.NewGuid().ToString();
        var thumbnailNames = new Fixture().CreateMany<string>(3).ToArray();

        await CreateImageDeleter().DeleteFiles(imageId, imageName, thumbnailNames, CancellationToken.None);

        _blobRepositoryMock.Verify(x => x.TryDeleteImage(imageName, ImageSizeType.Original, CancellationToken.None), Times.Once);
        foreach (var thumbnailName in thumbnailNames)
        {
            _blobRepositoryMock.Verify(x => x.TryDeleteImage(thumbnailName, ImageSizeType.Thumbnail, CancellationToken.None), Times.Once);
        }
    }

    [Fact]
    public async Task DeleteFiles_ImageWithoutThumbnails_ShouldBeDeleted()
    {
        _blobRepositoryMock.Setup(x => x.TryDeleteImage(It.IsAny<string>(),
            It.IsAny<ImageSizeType>(), CancellationToken.None));

        var imageId = Guid.NewGuid().ToString();
        var imageName = Guid.NewGuid().ToString();

        await CreateImageDeleter().DeleteFiles(imageId, imageName, [], CancellationToken.None);

        _blobRepositoryMock.Verify(x => x.TryDeleteImage(imageName, ImageSizeType.Original, CancellationToken.None), Times.Once);
        _blobRepositoryMock.Verify(x => x.TryDeleteImage(It.IsAny<string>(), ImageSizeType.Thumbnail, CancellationToken.None), Times.Never);
    }
}
