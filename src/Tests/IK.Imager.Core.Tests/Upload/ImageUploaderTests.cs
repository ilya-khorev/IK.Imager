using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Upload;
using IK.Imager.Core.Upload;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.Upload;

public class ImageUploaderTests
{
    private const string ImageId = "image-id";
    private const string ImageName = "image-id.jpg";
    private const string ImageGroup = "test-group";
    private const string Hash = "hash";

    private static readonly Uri BlobUrl = new("https://blobs.test/images/image-id.jpg");
    private static readonly Uri PublicUrl = new("https://cdn.test/images/image-id.jpg");
    private static readonly ImageFormat Jpeg = new("image/jpeg", ".jpg", ImageType.JPEG);
    private static readonly ImageSize Size = new(800, 600, 12345);

    private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock;
    private readonly Mock<IImageInspector> _imageInspectorMock;
    private readonly Mock<IImageValidator> _imageValidatorMock;
    private readonly Mock<IImageNameGenerator> _imageNameGeneratorMock;
    private readonly Mock<IImageUrlBuilder> _imageUrlBuilderMock;
    private readonly Mock<IImageEvents> _imageEventsMock;
    private readonly ImageDownloader _imageDownloader;
    private readonly ILogger<ImageUploader> _logger;

    public ImageUploaderTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<ImageUploader>();
        _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
        _blobRepositoryMock = new Mock<IImageBlobRepository>();
        _imageInspectorMock = new Mock<IImageInspector>();
        _imageValidatorMock = new Mock<IImageValidator>();
        _imageNameGeneratorMock = new Mock<IImageNameGenerator>();
        _imageUrlBuilderMock = new Mock<IImageUrlBuilder>();
        _imageEventsMock = new Mock<IImageEvents>();
        _imageDownloader = new ImageDownloader(new HttpClient());

        _imageUrlBuilderMock.Setup(x => x.Build(ImageName, ImageVariant.Original)).Returns(PublicUrl);

        _imageInspectorMock.Setup(x => x.DetectFormat(It.IsAny<Stream>())).Returns(Jpeg);
        _imageInspectorMock.Setup(x => x.ReadSize(It.IsAny<Stream>())).Returns(Size);
        _imageValidatorMock.Setup(x => x.CheckFormat(It.IsAny<ImageFormat?>())).Returns(ImageValidationResult.Success);
        _imageValidatorMock.Setup(x => x.CheckSize(It.IsAny<ImageSize>())).Returns(ImageValidationResult.Success);
        _imageNameGeneratorMock.Setup(x => x.NewImageId()).Returns(ImageId);
        _imageNameGeneratorMock.Setup(x => x.ToFileName(ImageId, Jpeg.FileExtension)).Returns(ImageName);
        _blobRepositoryMock
            .Setup(x => x.UploadImage(ImageName, It.IsAny<Stream>(), ImageVariant.Original, Jpeg.MimeType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobUploadResult
            {
                Hash = Hash,
                DateAdded = DateTimeOffset.UnixEpoch,
                Url = BlobUrl
            });
    }

    private ImageUploader CreateUploader() =>
        new(_logger, _imageInspectorMock.Object, _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
            _imageValidatorMock.Object, _imageNameGeneratorMock.Object, _imageDownloader,
            _imageUrlBuilderMock.Object, _imageEventsMock.Object);

    [Fact]
    public async Task Upload_ValidImage_ReturnsDetailsOfTheStoredImage()
    {
        var result = await CreateUploader().Upload(new MemoryStream([1, 2, 3]), ImageGroup, CancellationToken.None);

        Assert.Equal(ImageId, result.Id);
        Assert.Equal(ImageName, result.Name);
        Assert.Equal(Hash, result.Hash);
        //the built public url, not the raw blob url the repository reported
        Assert.Equal(PublicUrl, result.Url);
        Assert.Equal(Jpeg.MimeType, result.MimeType);
        Assert.Equal(Size.Width, result.Width);
        Assert.Equal(Size.Height, result.Height);
        Assert.Equal(Size.Bytes, result.Bytes);
    }

    /// <summary>
    /// The blob goes first and the metadata second - an image whose metadata never lands is invisible,
    /// which is recoverable, whereas metadata pointing at a blob that does not exist is not.
    /// </summary>
    [Fact]
    public async Task Upload_ValidImage_StoresBlobThenMetadata()
    {
        await CreateUploader().Upload(new MemoryStream([1, 2, 3]), ImageGroup, CancellationToken.None);

        _blobRepositoryMock.Verify(
            x => x.UploadImage(ImageName, It.IsAny<Stream>(), ImageVariant.Original, Jpeg.MimeType,
                It.IsAny<CancellationToken>()), Times.Once);

        _metadataRepositoryMock.Verify(x => x.SetMetadata(
            It.Is<ImageMetadata>(m =>
                m.Id == ImageId &&
                m.Name == ImageName &&
                m.ImageGroup == ImageGroup &&
                m.MD5Hash == Hash &&
                m.MimeType == Jpeg.MimeType &&
                m.ImageType == ImageType.JPEG &&
                m.FileExtension == Jpeg.FileExtension &&
                m.Width == Size.Width &&
                m.Height == Size.Height &&
                m.SizeBytes == Size.Bytes),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Thumbnail generation hangs off this event, so an upload that does not raise it produces an image
    /// that never gets thumbnails.
    /// </summary>
    [Fact]
    public async Task Upload_ValidImage_RaisesImageUploaded()
    {
        await CreateUploader().Upload(new MemoryStream([1, 2, 3]), ImageGroup, CancellationToken.None);

        _imageEventsMock.Verify(x => x.ImageUploaded(ImageId, ImageGroup, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_InvalidFormat_ThrowsAndStoresNothing()
    {
        _imageValidatorMock.Setup(x => x.CheckFormat(It.IsAny<ImageFormat?>()))
            .Returns(new ImageValidationResult(new ImageValidationError("key", "unsupported")));

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            CreateUploader().Upload(new MemoryStream([1, 2, 3]), ImageGroup, CancellationToken.None));

        _blobRepositoryMock.Verify(x => x.UploadImage(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<ImageVariant>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _metadataRepositoryMock.Verify(
            x => x.SetMetadata(It.IsAny<ImageMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _imageEventsMock.Verify(
            x => x.ImageUploaded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
