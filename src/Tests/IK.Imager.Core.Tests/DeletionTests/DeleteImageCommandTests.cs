using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.DeletionTests;

public class DeleteImageCommandTests
{
    private readonly ILogger<DeleteImageCommandHandler> _logger;
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock;

    public DeleteImageCommandTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<DeleteImageCommandHandler>();
        _blobRepositoryMock = new Mock<IImageBlobRepository>();
    }

    [Fact]
    public async Task ImageWithThumbnails_ShouldBeDeleted()
    {
        _blobRepositoryMock.Setup(x => x.TryDeleteImage(It.IsAny<string>(),
            It.IsAny<ImageSizeType>(), CancellationToken.None));
        
        DeleteImageCommand request = new Fixture().Create<DeleteImageCommand>();
        var commandHandler = new DeleteImageCommandHandler(_logger, _blobRepositoryMock.Object);
        await commandHandler.Handle(request, CancellationToken.None);
        
        _blobRepositoryMock.Verify(x => x.TryDeleteImage(request.ImageName, ImageSizeType.Original, CancellationToken.None), Times.Once);
        foreach (var thumbnailName in request.ThumbnailNames)
        {
            _blobRepositoryMock.Verify(x => x.TryDeleteImage(thumbnailName, ImageSizeType.Thumbnail, CancellationToken.None), Times.Once);
        }
    }
    
    [Fact]
    public async Task ImageWithoutThumbnails_ShouldBeDeleted()
    {
        _blobRepositoryMock.Setup(x => x.TryDeleteImage(It.IsAny<string>(),
            It.IsAny<ImageSizeType>(), CancellationToken.None));

        DeleteImageCommand request =
            new DeleteImageCommand(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new string[] { });
        var commandHandler = new DeleteImageCommandHandler(_logger, _blobRepositoryMock.Object);
        await commandHandler.Handle(request, CancellationToken.None);
        
        _blobRepositoryMock.Verify(x => x.TryDeleteImage(request.ImageName, ImageSizeType.Original, CancellationToken.None), Times.Once);
        _blobRepositoryMock.Verify(x => x.TryDeleteImage(It.IsAny<string>(), ImageSizeType.Thumbnail, CancellationToken.None), Times.Never);
    }
}