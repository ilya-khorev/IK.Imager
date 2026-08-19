using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Delete;
using IK.Imager.Core.Delete;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Moq;
using Xunit;

namespace IK.Imager.Core.Tests.Delete;

public class CdnImageDeleterTests
{
    private const string BlobHost = "https://ikimagesstorageaccount.blob.core.windows.net";
    private const string CdnHost = "https://ikimager.azureedge.net";

    private readonly Mock<IImageDeleter> _innerDeleterMock = new();
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock = new();
    private readonly Mock<ICdnUrlRewriter> _cdnUrlRewriterMock = new();
    private readonly Mock<ICdnPurger> _cdnPurgerMock = new();

    public CdnImageDeleterTests()
    {
        _blobRepositoryMock.Setup(x => x.GetImageUri(It.IsAny<string>(), It.IsAny<ImageVariant>()))
            .Returns((string name, ImageVariant variant) =>
                new Uri($"{BlobHost}/{ContainerOf(variant)}/{name}.jpg"));

        _cdnUrlRewriterMock.Setup(x => x.Rewrite(It.IsAny<Uri>()))
            .Returns((Uri uri) => new Uri(CdnHost + uri.AbsolutePath));
    }

    private static string ContainerOf(ImageVariant variant) =>
        variant == ImageVariant.Original ? "images" : "thumbnails";

    private CdnImageDeleter CreateCdnImageDeleter() =>
        new(_innerDeleterMock.Object, _blobRepositoryMock.Object, _cdnUrlRewriterMock.Object, _cdnPurgerMock.Object);

    private IReadOnlyCollection<Uri> CapturePurgedUris()
    {
        List<Uri> purged = [];
        _cdnPurgerMock.Setup(x => x.Purge(It.IsAny<IReadOnlyCollection<Uri>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Uri>, CancellationToken>((uris, _) => purged.AddRange(uris))
            .Returns(Task.CompletedTask);

        return purged;
    }

    [Fact]
    public async Task DeleteFiles_ImageWithThumbnails_PurgesOriginalAndThumbnailsInOneCall()
    {
        var purged = CapturePurgedUris();

        var imageName = Guid.NewGuid().ToString();
        var thumbnailNames = new Fixture().CreateMany<string>(3).ToArray();

        await CreateCdnImageDeleter().DeleteFiles(Guid.NewGuid().ToString(), imageName, thumbnailNames,
            CancellationToken.None);

        _cdnPurgerMock.Verify(x => x.Purge(It.IsAny<IReadOnlyCollection<Uri>>(), CancellationToken.None), Times.Once);

        Assert.Equal(
            new[] { $"{CdnHost}/images/{imageName}.jpg" }
                .Concat(thumbnailNames.Select(x => $"{CdnHost}/thumbnails/{x}.jpg")),
            purged.Select(x => x.ToString()));
    }

    [Fact]
    public async Task DeleteFiles_ImageWithoutThumbnails_PurgesOriginalOnly()
    {
        var purged = CapturePurgedUris();

        var imageName = Guid.NewGuid().ToString();

        await CreateCdnImageDeleter().DeleteFiles(Guid.NewGuid().ToString(), imageName, [], CancellationToken.None);

        Assert.Equal($"{CdnHost}/images/{imageName}.jpg", Assert.Single(purged).ToString());
    }

    [Fact]
    public async Task DeleteFiles_Always_PurgesAfterTheBlobsAreDeleted()
    {
        List<string> calls = [];

        _innerDeleterMock.Setup(x => x.DeleteFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("blobs"))
            .Returns(Task.CompletedTask);

        _cdnPurgerMock.Setup(x => x.Purge(It.IsAny<IReadOnlyCollection<Uri>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("purge"))
            .Returns(Task.CompletedTask);

        await CreateCdnImageDeleter().DeleteFiles(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
            [Guid.NewGuid().ToString()], CancellationToken.None);

        Assert.Equal(new[] { "blobs", "purge" }, calls);
    }

    [Fact]
    public async Task DeleteFiles_InnerDeleterThrows_DoesNotPurge()
    {
        _innerDeleterMock.Setup(x => x.DeleteFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateCdnImageDeleter()
            .DeleteFiles(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), [], CancellationToken.None));

        _cdnPurgerMock.Verify(x => x.Purge(It.IsAny<IReadOnlyCollection<Uri>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteMetadata_ImageExists_DelegatesWithoutPurging()
    {
        var imageId = Guid.NewGuid().ToString();
        var imageGroup = Guid.NewGuid().ToString();

        _innerDeleterMock.Setup(x => x.DeleteMetadata(imageId, imageGroup, CancellationToken.None))
            .ReturnsAsync(true);

        Assert.True(await CreateCdnImageDeleter().DeleteMetadata(imageId, imageGroup, CancellationToken.None));

        _cdnPurgerMock.Verify(x => x.Purge(It.IsAny<IReadOnlyCollection<Uri>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
