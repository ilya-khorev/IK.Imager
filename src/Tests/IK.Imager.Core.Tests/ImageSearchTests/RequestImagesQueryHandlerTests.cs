using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.ImageSearch;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.ImageSearchTests;

public class RequestImagesQueryHandlerTests
{
    private readonly ILogger<RequestImagesQueryHandler> _logger;
    private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock;

    public RequestImagesQueryHandlerTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<RequestImagesQueryHandler>();
        _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
        _blobRepositoryMock = new Mock<IImageBlobRepository>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task Handle_FindImages(int imagesCount)
    {
        _blobRepositoryMock.Setup(x => x.GetImageUri(It.IsAny<string>(), It.IsAny<ImageSizeType>()))
            .Returns(new Uri("https://test.com"));
        
        List<ImageMetadata> imageMetadataList = new Fixture().CreateMany<ImageMetadata>(imagesCount).ToList();
        
        _metadataRepositoryMock.Setup(x => x.GetMetadata(
                It.IsAny<ICollection<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(imageMetadataList);
        
        RequestImagesQueryHandler handler = new RequestImagesQueryHandler(_logger, _metadataRepositoryMock.Object, _blobRepositoryMock.Object);
        var result = await handler.Handle( new Fixture().Create<RequestImagesQuery>(), CancellationToken.None);

        CompareFields(imageMetadataList, result);
    }
 
    private void CompareFields(List<ImageMetadata> expectedImages, ImagesSearchResult actualImages)
    {
        for (int i = 0; i < expectedImages.Count; i++)
        {
            CompareFields(expectedImages[i], actualImages.Images[i]);
        }
    }
    
    private void CompareFields(ImageMetadata expectedImage, ImageFullInfoWithThumbnails actualImage)
    {
        Assert.Equal(expectedImage.SizeBytes, actualImage.Bytes);
        Assert.Equal(expectedImage.MD5Hash, actualImage.Hash);
        Assert.Equal(expectedImage.Height, actualImage.Height);
        Assert.Equal(expectedImage.Width, actualImage.Width);
        Assert.Equal(expectedImage.Tags, actualImage.Tags);
        Assert.Equal(expectedImage.DateAddedUtc, actualImage.DateAdded);
        Assert.Equal(expectedImage.MimeType, actualImage.MimeType);
        Assert.NotNull(actualImage.Url);
        Assert.True(actualImage.Thumbnails.Any());
    
        foreach (var imageThumbnail in actualImage.Thumbnails)
        {
            var matchedThumbnail = expectedImage.Thumbnails.Single(x => x.Id == imageThumbnail.Id);
            Assert.Equal(matchedThumbnail.SizeBytes, imageThumbnail.Bytes);
            Assert.Equal(matchedThumbnail.MD5Hash, imageThumbnail.Hash);
            Assert.Equal(matchedThumbnail.Height, imageThumbnail.Height);
            Assert.Equal(matchedThumbnail.Width, imageThumbnail.Width);
            Assert.Equal(matchedThumbnail.DateAddedUtc, imageThumbnail.DateAdded);
            Assert.Equal(matchedThumbnail.MimeType, imageThumbnail.MimeType);
            Assert.NotNull(imageThumbnail.Url);
        }
    }
}