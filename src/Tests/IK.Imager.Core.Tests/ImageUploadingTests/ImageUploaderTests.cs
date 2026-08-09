using System.Net.Http;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.ImageUploadingTests;

public class ImageUploaderTests
{
    private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock;
    private readonly Mock<IImageMetadataReader> _metadataReadeMock;
    private readonly Mock<IImageValidator> _imageValidatorMock;
    private readonly Mock<IImageIdentifierProvider> _imageIdentifierProvider;
    private readonly Mock<IImageEvents> _imageEventsMock;
    private readonly ImageDownloadClient _imageDownloadClient;
    private readonly ILogger<ImageUploader> _logger;

    public ImageUploaderTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<ImageUploader>();
        _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
        _blobRepositoryMock = new Mock<IImageBlobRepository>();
        _metadataReadeMock = new Mock<IImageMetadataReader>();
        _imageValidatorMock = new Mock<IImageValidator>();
        _imageIdentifierProvider = new Mock<IImageIdentifierProvider>();
        _imageEventsMock = new Mock<IImageEvents>();
        _imageDownloadClient = new ImageDownloadClient(new HttpClient());
    }

    [Fact]
    public async Task Upload_ValidImage_Uploaded()
    {
        //TODO
        ImageUploader imageUploader = new ImageUploader(_logger, _metadataReadeMock.Object, _blobRepositoryMock.Object,
            _metadataRepositoryMock.Object, _imageValidatorMock.Object, _imageIdentifierProvider.Object,
            _imageDownloadClient, _imageEventsMock.Object);
        await Task.CompletedTask;
    }
}