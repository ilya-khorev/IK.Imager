using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Core.ImageUploading;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.ImageUploadingTests;

public class UploadImageCommandHandlerTests
{
    private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock;
    private readonly Mock<IImageMetadataReader> _metadataReadeMock;
    private readonly Mock<IImageValidator> _imageValidatorMock;
    private readonly Mock<IImageIdentifierProvider> _imageIdentifierProvider;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly ILogger<UploadImageCommandHandler> _logger;

    public UploadImageCommandHandlerTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<UploadImageCommandHandler>();
        _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
        _blobRepositoryMock = new Mock<IImageBlobRepository>();
        _metadataReadeMock = new Mock<IImageMetadataReader>();
        _imageValidatorMock = new Mock<IImageValidator>();
        _imageIdentifierProvider = new Mock<IImageIdentifierProvider>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
    }

    [Fact]
    public async Task Handle_ValidImage_Uploaded()
    {
        //TODO
        UploadImageCommandHandler handler = new UploadImageCommandHandler(_logger, _metadataReadeMock.Object, _blobRepositoryMock.Object,
            _metadataRepositoryMock.Object, _imageValidatorMock.Object, _imageIdentifierProvider.Object, _domainEventDispatcherMock.Object);
        await Task.CompletedTask;
    }
}