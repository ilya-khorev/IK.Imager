using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.AzureBlobs;
using IK.Imager.TestsBase;
using Xunit;

namespace IK.Imager.Storage.AzureBlobs.Tests;

// These tests require a running Docker daemon - Azurite is started automatically by Testcontainers,
// see AzureBlobStorageFixture. Nothing has to be installed or launched by hand.
//
// Naming convention:
// - The name of the method being tested
// - The scenario under which it's being tested (optional)
// - The expected behavior when the scenario is invoked
[Trait("Category", "Integration")]
[Collection(AzuriteCollection.Name)]
public class AzureBlobImageRepositoryTests
{
    private readonly AzureBlobStorageFixture _fixture;
    private readonly AzureBlobImageRepository _imageBlobAzureRepository;

    private const string TestImagesFolder = "Images";
    private const string JpegType = "image/jpeg";

    /// <summary>
    /// Ordered so that an index always refers to the same file - the container isolation tests
    /// rely on being able to ask for two different images.
    /// </summary>
    private static readonly string[] TestImages = Directory
        .GetFiles(TestImagesFolder)
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToArray();

    public AzureBlobImageRepositoryTests(AzureBlobStorageFixture fixture)
    {
        _fixture = fixture;
        _imageBlobAzureRepository = fixture.Repository;
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task UploadImage_ReturnsUrlAndHash(ImageVariant imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);

        Assert.NotNull(uploadImageResult.Item2.Url);
        Assert.NotNull(uploadImageResult.Item2.Hash);
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task UploadImage_ReturnsHashMatchingContentMd5(ImageVariant imageType)
    {
        var imageBytes = await File.ReadAllBytesAsync(TestImages[0]);
        var blobPath = GenerateUniqueBlobPath();
        await using var imageStream = new MemoryStream(imageBytes);

        var uploadImageResult = await _imageBlobAzureRepository.UploadImage(blobPath, imageStream, imageType, JpegType, CancellationToken.None);

        Assert.Equal(Convert.ToBase64String(MD5.HashData(imageBytes)), uploadImageResult.Hash);
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task UploadImage_ExistingImageName_ThrowsRequestFailedException(ImageVariant imageType)
    {
        var (blobPath, _) = await UploadTestImage(imageType);

        await using var fileStream = OpenTestImageForReading();
        var exception = await Assert.ThrowsAsync<RequestFailedException>(() =>
            _imageBlobAzureRepository.UploadImage(blobPath, fileStream, imageType, JpegType, CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.Status);
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task UploadImage_EmptyImageName_ThrowsArgumentException(ImageVariant imageType)
    {
        await using var fileStream = OpenTestImageForReading();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageBlobAzureRepository.UploadImage(string.Empty, fileStream, imageType, JpegType, CancellationToken.None));
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task UploadImage_NullStream_ThrowsArgumentNullException(ImageVariant imageType)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _imageBlobAzureRepository.UploadImage(GenerateUniqueBlobPath(), null!, imageType, JpegType, CancellationToken.None));
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task ImageExists_UploadedTestImage_ReturnsTrue(ImageVariant imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);

        bool imageExists = await _imageBlobAzureRepository.ImageExists(uploadImageResult.Item1, imageType, CancellationToken.None);

        Assert.True(imageExists);
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task ImageExists_NotExistingImage_ReturnsFalse(ImageVariant imageType)
    {
        string blobPath = GenerateUniqueBlobPath();

        bool imageExists = await _imageBlobAzureRepository.ImageExists(blobPath, imageType, CancellationToken.None);

        Assert.False(imageExists);
    }

    [Fact]
    public async Task ImageExists_UploadedAsOriginal_ReturnsFalseForThumbnail()
    {
        var (blobPath, _) = await UploadTestImage(ImageVariant.Original);

        bool thumbnailExists = await _imageBlobAzureRepository.ImageExists(blobPath, ImageVariant.Thumbnail, CancellationToken.None);

        Assert.False(thumbnailExists);
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task DownloadImage_ReturnsCorrectStream(ImageVariant imageType)
    {
        await using var fileStream = OpenTestImageForReading();
        await using MemoryStream imageStream = new MemoryStream();
        await fileStream.CopyToAsync(imageStream);
        imageStream.Position = 0;
        string blobPath = GenerateUniqueBlobPath();
        await _imageBlobAzureRepository.UploadImage(blobPath, imageStream, imageType, JpegType, CancellationToken.None);

        await using var downloadedImageStream = await _imageBlobAzureRepository.DownloadImage(blobPath, imageType, CancellationToken.None);

        Assert.NotNull(downloadedImageStream);
        Assert.True(CompareMemoryStreams(imageStream, downloadedImageStream));
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task DownloadImage_NotExistingImage_ReturnsNull(ImageVariant imageType)
    {
        var downloadedImageStream =
            await _imageBlobAzureRepository.DownloadImage(GenerateUniqueBlobPath(), imageType, CancellationToken.None);

        Assert.Null(downloadedImageStream);
    }

    /// <summary>
    /// Originals and thumbnails live in two separate blob containers, so the very same image name
    /// must be able to hold two unrelated images.
    /// </summary>
    [Fact]
    public async Task UploadImage_SameNameInOriginalAndThumbnail_StoresIndependentBlobs()
    {
        var sharedImageName = GenerateUniqueBlobPath();
        var originalBytes = await File.ReadAllBytesAsync(TestImages[0]);
        var thumbnailBytes = await File.ReadAllBytesAsync(TestImages[1]);

        await using (var originalStream = new MemoryStream(originalBytes))
            await _imageBlobAzureRepository.UploadImage(sharedImageName, originalStream, ImageVariant.Original, JpegType, CancellationToken.None);
        await using (var thumbnailStream = new MemoryStream(thumbnailBytes))
            await _imageBlobAzureRepository.UploadImage(sharedImageName, thumbnailStream, ImageVariant.Thumbnail, JpegType, CancellationToken.None);

        await using var downloadedOriginal = await _imageBlobAzureRepository.DownloadImage(sharedImageName, ImageVariant.Original, CancellationToken.None);
        await using var downloadedThumbnail = await _imageBlobAzureRepository.DownloadImage(sharedImageName, ImageVariant.Thumbnail, CancellationToken.None);

        Assert.NotNull(downloadedOriginal);
        Assert.NotNull(downloadedThumbnail);
        Assert.Equal(originalBytes, downloadedOriginal.ToArray());
        Assert.Equal(thumbnailBytes, downloadedThumbnail.ToArray());
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task GetImageUri_ReturnsCorrectUri(ImageVariant imageType)
    {
        var expectedLength = new FileInfo(TestImages[0]).Length;
        string blobPath = GenerateUniqueBlobPath();
        await using (var fileStream = OpenTestImageForReading())
            await _imageBlobAzureRepository.UploadImage(blobPath, fileStream, imageType, JpegType, CancellationToken.None);

        var imageUri = _imageBlobAzureRepository.GetImageUri(blobPath, imageType);

        //The blob containers are created with public (blob level) access, so this is an anonymous request
        using HttpClient client = new HttpClient();
        await using Stream streamByUri = await client.GetStreamAsync(imageUri);
        await using MemoryStream memoryStreamByUri = new MemoryStream();
        await streamByUri.CopyToAsync(memoryStreamByUri);

        Assert.Equal(expectedLength, memoryStreamByUri.Length);
    }

    [Theory]
    [InlineData(ImageVariant.Original, Constants.AzureBlobStorage.ImagesContainerName)]
    [InlineData(ImageVariant.Thumbnail, Constants.AzureBlobStorage.ThumbnailsContainerName)]
    public async Task GetImageUri_UploadedImage_UriPointsAtExpectedContainer(ImageVariant imageType, string expectedContainerName)
    {
        var (blobPath, _) = await UploadTestImage(imageType);

        var imageUri = _imageBlobAzureRepository.GetImageUri(blobPath, imageType);

        Assert.Contains(expectedContainerName, imageUri.Segments.Select(x => x.Trim('/')));
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task TryDeleteImage_UploadedTestImage_ReturnsTrue(ImageVariant imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);

        var imageRemoved = await _imageBlobAzureRepository.TryDeleteImage(uploadImageResult.Item1, imageType, CancellationToken.None);

        Assert.True(imageRemoved);
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task TryDeleteImage_UploadedTestImage_ImageNoLongerExists(ImageVariant imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);
        await _imageBlobAzureRepository.TryDeleteImage(uploadImageResult.Item1, imageType, CancellationToken.None);

        var imageExists = await _imageBlobAzureRepository.ImageExists(uploadImageResult.Item1, imageType, CancellationToken.None);

        Assert.False(imageExists);
    }

    [Theory]
    [InlineData(ImageVariant.Original)]
    [InlineData(ImageVariant.Thumbnail)]
    public async Task TryDeleteImage_NotExistingImage_ReturnsFalse(ImageVariant imageType)
    {
        string blobPath = GenerateUniqueBlobPath();

        var imageRemoved = await _imageBlobAzureRepository.TryDeleteImage(blobPath, imageType, CancellationToken.None);

        Assert.False(imageRemoved);
    }

    [Fact]
    public async Task TryDeleteImage_UploadedAsOriginal_ThumbnailWithSameNameStillExists()
    {
        var sharedImageName = GenerateUniqueBlobPath();
        await using (var originalStream = OpenTestImageForReading())
            await _imageBlobAzureRepository.UploadImage(sharedImageName, originalStream, ImageVariant.Original, JpegType, CancellationToken.None);
        await using (var thumbnailStream = OpenTestImageForReading(1))
            await _imageBlobAzureRepository.UploadImage(sharedImageName, thumbnailStream, ImageVariant.Thumbnail, JpegType, CancellationToken.None);

        await _imageBlobAzureRepository.TryDeleteImage(sharedImageName, ImageVariant.Original, CancellationToken.None);

        Assert.False(await _imageBlobAzureRepository.ImageExists(sharedImageName, ImageVariant.Original, CancellationToken.None));
        Assert.True(await _imageBlobAzureRepository.ImageExists(sharedImageName, ImageVariant.Thumbnail, CancellationToken.None));
    }

    /// <summary>
    /// Both containers must be publicly readable, otherwise the urls returned by the api would not
    /// be usable by the clients.
    /// </summary>
    [Theory]
    [InlineData(Constants.AzureBlobStorage.ImagesContainerName)]
    [InlineData(Constants.AzureBlobStorage.ThumbnailsContainerName)]
    public async Task UploadImage_FirstUseOfContainer_ContainerHasPublicBlobAccess(string containerName)
    {
        //make sure the lazily created containers do exist by now
        await UploadTestImage(ImageVariant.Original);
        await UploadTestImage(ImageVariant.Thumbnail);

        var containerProperties = await _fixture.BlobServiceClient
            .GetBlobContainerClient(containerName)
            .GetPropertiesAsync();

        Assert.Equal(Azure.Storage.Blobs.Models.PublicAccessType.Blob, containerProperties.Value.PublicAccess);
    }

    /// <summary>
    /// The blobs are publicly reachable by url, so a browser fetching one has nothing but the stored
    /// content type to go on. Without it every image comes back as application/octet-stream.
    /// </summary>
    [Theory]
    [InlineData(ImageVariant.Original, Constants.AzureBlobStorage.ImagesContainerName)]
    [InlineData(ImageVariant.Thumbnail, Constants.AzureBlobStorage.ThumbnailsContainerName)]
    public async Task UploadImage_StoresTheGivenContentTypeOnTheBlob(ImageVariant imageType, string containerName)
    {
        var (blobPath, _) = await UploadTestImage(imageType);

        var blobProperties = await _fixture.BlobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobPath)
            .GetPropertiesAsync();

        Assert.Equal(JpegType, blobProperties.Value.ContentType);
    }

    private async Task<(string, BlobUploadResult)> UploadTestImage(ImageVariant imageType)
    {
        await using var fileStream = OpenTestImageForReading();
        var blobPath = GenerateUniqueBlobPath();

        var uploadImageResult = await _imageBlobAzureRepository.UploadImage(blobPath, fileStream, imageType, JpegType, CancellationToken.None);
        return (blobPath, uploadImageResult);
    }

    private string GenerateUniqueBlobPath() => Guid.NewGuid().ToString();

    private static FileStream OpenTestImageForReading(int index = 0) =>
        File.Open(TestImages[index % TestImages.Length], FileMode.Open, FileAccess.Read);

    /// <summary>
    /// Comparing two streams byte by byte.
    /// Based on pretty straightforward implementation.
    /// It might consume a lot of memory if used for big size streams.
    /// </summary>
    /// <param name="ms1"></param>
    /// <param name="ms2"></param>
    /// <returns></returns>
    private bool CompareMemoryStreams(MemoryStream ms1, MemoryStream ms2)
    {
        if (ms1.Length != ms2.Length)
            return false;

        var msArray1 = ms1.ToArray();
        var msArray2 = ms2.ToArray();

        return msArray1.SequenceEqual(msArray2);
    }
}
