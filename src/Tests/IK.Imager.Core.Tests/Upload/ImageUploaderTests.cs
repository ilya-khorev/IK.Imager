using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Divergic.Logging.Xunit;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Upload;
using IK.Imager.Core.Tests.Infrastructure;
using IK.Imager.Core.Upload;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.Upload;

public class ImageUploaderTests
{
    private const string ImageId = "image-id";
    private const string BlobPath = "image-id.jpg";
    private const string TenantId = "test-tenant";
    private const string Collection = "test-collection";
    private const string Hash = "hash";

    private static readonly Uri BlobUrl = new("https://blobs.test/images/image-id.jpg");
    private static readonly Uri PublicUrl = new("https://cdn.test/images/image-id.jpg");
    private static readonly ImageFormat Jpeg = new("image/jpeg", ".jpg", ImageType.JPEG);
    private static readonly ImageSize Size = new(800, 600, 12345);
    private static readonly ImageUploadOptions Options = new(Collection: Collection);

    private readonly Mock<IImageMetadataRepository> _metadataRepositoryMock;
    private readonly Mock<IImageBlobRepository> _blobRepositoryMock;
    private readonly Mock<IImageInspector> _imageInspectorMock;
    private readonly Mock<IImageNameGenerator> _imageNameGeneratorMock;
    private readonly Mock<IImageUrlBuilder> _imageUrlBuilderMock;
    private readonly Mock<IImageEvents> _imageEventsMock;
    private readonly ImageDownloader _imageDownloader;
    private readonly ICacheLogger<ImageUploader> _logger;

    public ImageUploaderTests(ITestOutputHelper output)
    {
        _logger = output.BuildLoggerFor<ImageUploader>();
        _metadataRepositoryMock = new Mock<IImageMetadataRepository>();
        _blobRepositoryMock = new Mock<IImageBlobRepository>();
        _imageInspectorMock = new Mock<IImageInspector>();
        _imageNameGeneratorMock = new Mock<IImageNameGenerator>();
        _imageUrlBuilderMock = new Mock<IImageUrlBuilder>();
        _imageEventsMock = new Mock<IImageEvents>();
        _imageDownloader = new ImageDownloader(new HttpClient(), ImageLimitations.WithMaxSizeBytes(int.MaxValue),
            DownloadSettings.WithMaxRedirects(), output.BuildLoggerFor<ImageDownloader>());

        _imageUrlBuilderMock.Setup(x => x.Build(BlobPath, ImageVariant.Original)).Returns(PublicUrl);

        //the id is free unless a test says otherwise
        _metadataRepositoryMock
            .Setup(x => x.GetMetadata(It.IsAny<ICollection<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _imageInspectorMock.Setup(x => x.Inspect(It.IsAny<Stream>())).Returns((Jpeg, Size));
        _imageNameGeneratorMock.Setup(x => x.NewImageId()).Returns(ImageId);
        _imageNameGeneratorMock
            .Setup(x => x.BuildBlobPath(TenantId, It.IsAny<string?>(), It.IsAny<string?>(), ImageId, Jpeg.FileExtension))
            .Returns(BlobPath);
        _blobRepositoryMock
            .Setup(x => x.UploadImage(BlobPath, It.IsAny<Stream>(), ImageVariant.Original, Jpeg.MimeType,
                false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobUploadResult
            {
                Hash = Hash,
                DateAdded = DateTimeOffset.UnixEpoch,
                Url = BlobUrl
            });
    }

    private ImageUploader CreateUploader() =>
        new(_logger, _imageInspectorMock.Object, _blobRepositoryMock.Object, _metadataRepositoryMock.Object,
            _imageNameGeneratorMock.Object, _imageDownloader, _imageUrlBuilderMock.Object, _imageEventsMock.Object);

    [Fact]
    public async Task Upload_ValidImage_ReturnsDetailsOfTheStoredImage()
    {
        var result = await CreateUploader().Upload(new MemoryStream([1, 2, 3]), TenantId, Options, CancellationToken.None);

        Assert.Equal(ImageId, result.Id);
        Assert.Equal(BlobPath, result.BlobPath);
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
        await CreateUploader().Upload(new MemoryStream([1, 2, 3]), TenantId, Options, CancellationToken.None);

        _blobRepositoryMock.Verify(
            x => x.UploadImage(BlobPath, It.IsAny<Stream>(), ImageVariant.Original, Jpeg.MimeType,
                false, It.IsAny<CancellationToken>()), Times.Once);

        _metadataRepositoryMock.Verify(x => x.CreateMetadata(
            It.Is<ImageMetadata>(m =>
                m.Id == ImageId &&
                m.BlobPath == BlobPath &&
                m.TenantId == TenantId &&
                m.Collection == Collection &&
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
        await CreateUploader().Upload(new MemoryStream([1, 2, 3]), TenantId, Options, CancellationToken.None);

        _imageEventsMock.Verify(x => x.ImageUploaded(TenantId, ImageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_RejectedImage_ThrowsAndStoresNothing()
    {
        _imageInspectorMock.Setup(x => x.Inspect(It.IsAny<Stream>()))
            .Throws(new System.ComponentModel.DataAnnotations.ValidationException("unsupported"));

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            CreateUploader().Upload(new MemoryStream([1, 2, 3]), TenantId, Options, CancellationToken.None));

        _blobRepositoryMock.Verify(x => x.UploadImage(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<ImageVariant>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _metadataRepositoryMock.Verify(
            x => x.CreateMetadata(It.IsAny<ImageMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _imageEventsMock.Verify(
            x => x.ImageUploaded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Metadata is what makes an image exist, so a taken id is refused before a blob is written.
    /// </summary>
    [Fact]
    public async Task Upload_IdAlreadyTaken_ThrowsAndStoresNothing()
    {
        _metadataRepositoryMock
            .Setup(x => x.GetMetadata(It.Is<ICollection<string>>(ids => ids.Contains(ImageId)), TenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImageMetadata { Id = ImageId, TenantId = TenantId }]);

        await Assert.ThrowsAsync<ImageAlreadyExistsException>(() =>
            CreateUploader().Upload(new MemoryStream([1, 2, 3]), TenantId, Options, CancellationToken.None));

        _blobRepositoryMock.Verify(x => x.UploadImage(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<ImageVariant>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _metadataRepositoryMock.Verify(
            x => x.CreateMetadata(It.IsAny<ImageMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        _imageEventsMock.Verify(
            x => x.ImageUploaded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Two uploads of one id can still race past that check. Without a unique prefix they share a blob path,
    /// so the loser must leave the blob alone - it is the one the winner's metadata now points at.
    /// </summary>
    [Fact]
    public async Task Upload_IdTakenWhileTheBlobWasWritten_LeavesTheBlobInPlace()
    {
        _metadataRepositoryMock
            .Setup(x => x.CreateMetadata(It.IsAny<ImageMetadata>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ImageAlreadyExistsException(TenantId, ImageId));

        await Assert.ThrowsAsync<ImageAlreadyExistsException>(() =>
            CreateUploader().Upload(new MemoryStream([1, 2, 3]), TenantId, Options, CancellationToken.None));

        _blobRepositoryMock.Verify(x => x.TryDeleteImage(It.IsAny<string>(), It.IsAny<ImageVariant>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _imageEventsMock.Verify(
            x => x.ImageUploaded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Deleting an image drops its metadata at once and its blobs off the bus a moment later, so an id can
    /// be free while its blob is still there.
    /// </summary>
    [Fact]
    public async Task Upload_OrphanedBlobAtThePath_OverwritesIt()
    {
        _blobRepositoryMock
            .Setup(x => x.UploadImage(BlobPath, It.IsAny<Stream>(), ImageVariant.Original, Jpeg.MimeType,
                false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BlobAlreadyExistsException(BlobPath));
        _blobRepositoryMock
            .Setup(x => x.UploadImage(BlobPath, It.IsAny<Stream>(), ImageVariant.Original, Jpeg.MimeType,
                true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlobUploadResult
            {
                Hash = Hash,
                DateAdded = DateTimeOffset.UnixEpoch,
                Url = BlobUrl
            });

        var result = await CreateUploader().Upload(new MemoryStream([1, 2, 3]), TenantId, Options, CancellationToken.None);

        Assert.Equal(ImageId, result.Id);
        _metadataRepositoryMock.Verify(
            x => x.CreateMetadata(It.Is<ImageMetadata>(m => m.Id == ImageId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Upload-by-url accepts any absolute url, so a caller can hand us a SAS whose signature is in the
    /// query string. Both url lines on this path go through the redaction.
    /// </summary>
    [Fact]
    public async Task UploadByUrl_SasUrl_DoesNotLogTheSignature()
    {
        const string sasUrl = "https://account.blob.core.windows.net/images/photo.jpg?sv=2024-11-04&sig=TOPSECRET";

        var downloaderMock = new Mock<IImageDownloader>();
        downloaderMock.Setup(x => x.GetMemoryStream(sasUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemoryStream?)null);

        var uploader = new ImageUploader(_logger, _imageInspectorMock.Object, _blobRepositoryMock.Object,
            _metadataRepositoryMock.Object, _imageNameGeneratorMock.Object, downloaderMock.Object,
            _imageUrlBuilderMock.Object, _imageEventsMock.Object);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            uploader.UploadByUrl(sasUrl, TenantId, Options, CancellationToken.None));

        Assert.NotEmpty(_logger.Entries);
        Assert.All(_logger.Entries, entry => Assert.DoesNotContain("TOPSECRET", entry.Message));
        Assert.All(_logger.Entries, entry => Assert.DoesNotContain("sig=", entry.Message));
    }
}
