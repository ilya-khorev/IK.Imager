using System;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Cdn;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IK.Imager.Core.Tests.Cdn;

public class ImageUrlBuilderTests
{
    private const string BlobPath = "d41be6cb6880421aa87fa401f79ed0f6fb1277.jpg";

    private static readonly Uri BlobUri =
        new($"https://ikimagesstorageaccount.blob.core.windows.net/images/{BlobPath}");

    private readonly Mock<IImageBlobRepository> _blobRepositoryMock = new();

    public ImageUrlBuilderTests()
    {
        _blobRepositoryMock.Setup(x => x.GetImageUri(BlobPath, ImageVariant.Original)).Returns(BlobUri);
    }

    private ImageUrlBuilder CreateImageUrlBuilder(Uri? cdnUri)
    {
        var optionsMock = new Mock<IOptions<CdnSettings>>();
        optionsMock.Setup(x => x.Value).Returns(new CdnSettings { Uri = cdnUri });

        return new ImageUrlBuilder(_blobRepositoryMock.Object, optionsMock.Object);
    }

    [Fact]
    public void Build_CdnConfigured_ReplacesTheBlobHostWithTheCdnHost()
    {
        IImageUrlBuilder imageUrlBuilder = CreateImageUrlBuilder(new Uri("https://ikimager.azureedge.net"));

        Assert.Equal(new Uri($"https://ikimager.azureedge.net/images/{BlobPath}"),
            imageUrlBuilder.Build(BlobPath, ImageVariant.Original));
    }

    [Fact]
    public void Build_CdnNotConfigured_ReturnsTheBlobUri()
    {
        IImageUrlBuilder imageUrlBuilder = CreateImageUrlBuilder(null);

        Assert.Equal(BlobUri, imageUrlBuilder.Build(BlobPath, ImageVariant.Original));
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public void Build_AnyVariant_AsksTheRepositoryForThatVariant(ImageVariant variant)
    {
        _blobRepositoryMock.Setup(x => x.GetImageUri(BlobPath, variant)).Returns(BlobUri);

        CreateImageUrlBuilder(null).Build(BlobPath, variant);

        _blobRepositoryMock.Verify(x => x.GetImageUri(BlobPath, variant), Times.Once);
    }
}
