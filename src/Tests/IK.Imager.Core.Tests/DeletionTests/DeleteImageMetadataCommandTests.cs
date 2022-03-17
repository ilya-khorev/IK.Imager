using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.DeletionTests;

public class DeleteImageMetadataCommandTests
{
    private readonly ILogger<DeleteImageMetadataCommandHandler> _logger;
    private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;

    public DeleteImageMetadataCommandTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<DeleteImageMetadataCommandHandler>();
        _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
        _mediatorMock = new Mock<IMediator>();
    }

    [Fact]
    public async Task ImageExists_ShouldBeDeleted()
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
        
        DeleteImageMetadataCommandHandler deleteImageMetadataCommandHandler =
            new DeleteImageMetadataCommandHandler(_logger, _metadataRepositoryMock.Object, _mediatorMock.Object);
        
        var result = await deleteImageMetadataCommandHandler.Handle(new Fixture().Create<DeleteImageMetadataCommand>(),
            CancellationToken.None);
        Assert.True(result);
        
        _mediatorMock.Verify(x => x.Publish(It.IsAny<ImageMetadataDeletedDomainEvent>(), CancellationToken.None), Times.Once);
    }
    
    [Fact]
    public async Task ImageDoesNotExist_ReturnsFalse()
    {
        _metadataRepositoryMock.Setup(x => x.GetMetadata(
                It.IsAny<ICollection<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImageMetadata>());

        DeleteImageMetadataCommandHandler deleteImageMetadataCommandHandler =
            new DeleteImageMetadataCommandHandler(_logger, _metadataRepositoryMock.Object, _mediatorMock.Object);

        var result = await deleteImageMetadataCommandHandler.Handle(new Fixture().Create<DeleteImageMetadataCommand>(),
            CancellationToken.None);
        Assert.False(result);
    }
}