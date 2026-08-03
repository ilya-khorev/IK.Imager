using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.TestsBase;
using Xunit;

namespace IK.Imager.ImageStorage.AzureFiles.Tests;

// These tests require a running Docker daemon - Azurite is started automatically by Testcontainers,
// see ImageBlobsStorageFixture. Nothing has to be installed or launched by hand.
//
// Naming convention:
// - The name of the method being tested
// - The scenario under which it's being tested (optional)
// - The expected behavior when the scenario is invoked
[Trait("Category", "Integration")]
[Collection(AzuriteCollection.Name)]
public class ImageAzureBlobRepositoryEmulatorTests
{
    private readonly ImageBlobsStorageFixture _fixture;
    private readonly ImageBlobAzureRepository _imageBlobAzureRepository;

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

    public ImageAzureBlobRepositoryEmulatorTests(ImageBlobsStorageFixture fixture)
    {
        _fixture = fixture;
        _imageBlobAzureRepository = fixture.BlobImageRepository;
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task UploadImage_ReturnsUrlAndHash(ImageSizeType imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);

        Assert.NotNull(uploadImageResult.Item2.Url);
        Assert.NotNull(uploadImageResult.Item2.Hash);
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task UploadImage_ReturnsHashMatchingContentMd5(ImageSizeType imageType)
    {
        var imageBytes = await File.ReadAllBytesAsync(TestImages[0]);
        var imageName = GenerateUniqueImageName();
        await using var imageStream = new MemoryStream(imageBytes);

        var uploadImageResult = await _imageBlobAzureRepository.UploadImage(imageName, imageStream, imageType, JpegType, CancellationToken.None);

        Assert.Equal(Convert.ToBase64String(MD5.HashData(imageBytes)), uploadImageResult.Hash);
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task UploadImage_ExistingImageName_ThrowsRequestFailedException(ImageSizeType imageType)
    {
        var (imageName, _) = await UploadTestImage(imageType);

        await using var fileStream = OpenTestImageForReading();
        var exception = await Assert.ThrowsAsync<RequestFailedException>(() =>
            _imageBlobAzureRepository.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.Conflict, exception.Status);
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task UploadImage_EmptyImageName_ThrowsArgumentException(ImageSizeType imageType)
    {
        await using var fileStream = OpenTestImageForReading();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _imageBlobAzureRepository.UploadImage(string.Empty, fileStream, imageType, JpegType, CancellationToken.None));
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task UploadImage_NullStream_ThrowsArgumentNullException(ImageSizeType imageType)
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _imageBlobAzureRepository.UploadImage(GenerateUniqueImageName(), null!, imageType, JpegType, CancellationToken.None));
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task ImageExists_UploadedTestImage_ReturnsTrue(ImageSizeType imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);

        bool imageExists = await _imageBlobAzureRepository.ImageExists(uploadImageResult.Item1, imageType, CancellationToken.None);

        Assert.True(imageExists);
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task ImageExists_NotExistingImage_ReturnsFalse(ImageSizeType imageType)
    {
        string imageName = GenerateUniqueImageName();

        bool imageExists = await _imageBlobAzureRepository.ImageExists(imageName, imageType, CancellationToken.None);

        Assert.False(imageExists);
    }

    [Fact]
    public async Task ImageExists_UploadedAsOriginal_ReturnsFalseForThumbnail()
    {
        var (imageName, _) = await UploadTestImage(ImageSizeType.Original);

        bool thumbnailExists = await _imageBlobAzureRepository.ImageExists(imageName, ImageSizeType.Thumbnail, CancellationToken.None);

        Assert.False(thumbnailExists);
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task DownloadImage_ReturnsCorrectStream(ImageSizeType imageType)
    {
        await using var fileStream = OpenTestImageForReading();
        await using MemoryStream imageStream = new MemoryStream();
        await fileStream.CopyToAsync(imageStream);
        imageStream.Position = 0;
        string imageName = GenerateUniqueImageName();
        await _imageBlobAzureRepository.UploadImage(imageName, imageStream, imageType, JpegType, CancellationToken.None);

        await using var downloadedImageStream = await _imageBlobAzureRepository.DownloadImage(imageName, imageType, CancellationToken.None);

        Assert.NotNull(downloadedImageStream);
        Assert.True(CompareMemoryStreams(imageStream, downloadedImageStream));
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task DownloadImage_NotExistingImage_ThrowsRequestFailedException(ImageSizeType imageType)
    {
        var exception = await Assert.ThrowsAsync<RequestFailedException>(() =>
            _imageBlobAzureRepository.DownloadImage(GenerateUniqueImageName(), imageType, CancellationToken.None));

        Assert.Equal((int)HttpStatusCode.NotFound, exception.Status);
    }

    /// <summary>
    /// Originals and thumbnails live in two separate blob containers, so the very same image name
    /// must be able to hold two unrelated images.
    /// </summary>
    [Fact]
    public async Task UploadImage_SameNameInOriginalAndThumbnail_StoresIndependentBlobs()
    {
        var sharedImageName = GenerateUniqueImageName();
        var originalBytes = await File.ReadAllBytesAsync(TestImages[0]);
        var thumbnailBytes = await File.ReadAllBytesAsync(TestImages[1]);

        await using (var originalStream = new MemoryStream(originalBytes))
            await _imageBlobAzureRepository.UploadImage(sharedImageName, originalStream, ImageSizeType.Original, JpegType, CancellationToken.None);
        await using (var thumbnailStream = new MemoryStream(thumbnailBytes))
            await _imageBlobAzureRepository.UploadImage(sharedImageName, thumbnailStream, ImageSizeType.Thumbnail, JpegType, CancellationToken.None);

        await using var downloadedOriginal = await _imageBlobAzureRepository.DownloadImage(sharedImageName, ImageSizeType.Original, CancellationToken.None);
        await using var downloadedThumbnail = await _imageBlobAzureRepository.DownloadImage(sharedImageName, ImageSizeType.Thumbnail, CancellationToken.None);

        Assert.NotNull(downloadedOriginal);
        Assert.NotNull(downloadedThumbnail);
        Assert.Equal(originalBytes, downloadedOriginal.ToArray());
        Assert.Equal(thumbnailBytes, downloadedThumbnail.ToArray());
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task GetImageUri_ReturnsCorrectUri(ImageSizeType imageType)
    {
        var expectedLength = new FileInfo(TestImages[0]).Length;
        string imageName = GenerateUniqueImageName();
        await using (var fileStream = OpenTestImageForReading())
            await _imageBlobAzureRepository.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);

        var imageUri = _imageBlobAzureRepository.GetImageUri(imageName, imageType);

        //The blob containers are created with public (blob level) access, so this is an anonymous request
        using HttpClient client = new HttpClient();
        await using Stream streamByUri = await client.GetStreamAsync(imageUri);
        await using MemoryStream memoryStreamByUri = new MemoryStream();
        await streamByUri.CopyToAsync(memoryStreamByUri);

        Assert.Equal(expectedLength, memoryStreamByUri.Length);
    }

    [Theory]
    [InlineData(ImageSizeType.Original, Constants.AzureBlobStorage.ImagesContainerName)]
    [InlineData(ImageSizeType.Thumbnail, Constants.AzureBlobStorage.ThumbnailsContainerName)]
    public async Task GetImageUri_UploadedImage_UriPointsAtExpectedContainer(ImageSizeType imageType, string expectedContainerName)
    {
        var (imageName, _) = await UploadTestImage(imageType);

        var imageUri = _imageBlobAzureRepository.GetImageUri(imageName, imageType);

        Assert.Contains(expectedContainerName, imageUri.Segments.Select(x => x.Trim('/')));
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task DeleteImage_UploadedTestImage_ReturnsTrue(ImageSizeType imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);

        var imageRemoved = await _imageBlobAzureRepository.TryDeleteImage(uploadImageResult.Item1, imageType, CancellationToken.None);

        Assert.True(imageRemoved);
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task DeleteImage_UploadedTestImage_ImageNoLongerExist(ImageSizeType imageType)
    {
        var uploadImageResult = await UploadTestImage(imageType);
        await _imageBlobAzureRepository.TryDeleteImage(uploadImageResult.Item1, imageType, CancellationToken.None);

        var imageExists = await _imageBlobAzureRepository.ImageExists(uploadImageResult.Item1, imageType, CancellationToken.None);

        Assert.False(imageExists);
    }

    [Theory]
    [InlineData(ImageSizeType.Original)]
    [InlineData(ImageSizeType.Thumbnail)]
    public async Task DeleteImage_NotExistingImage_ReturnsFalse(ImageSizeType imageType)
    {
        string imageName = GenerateUniqueImageName();

        var imageRemoved = await _imageBlobAzureRepository.TryDeleteImage(imageName, imageType, CancellationToken.None);

        Assert.False(imageRemoved);
    }

    [Fact]
    public async Task DeleteImage_UploadedAsOriginal_ThumbnailWithSameNameStillExists()
    {
        var sharedImageName = GenerateUniqueImageName();
        await using (var originalStream = OpenTestImageForReading())
            await _imageBlobAzureRepository.UploadImage(sharedImageName, originalStream, ImageSizeType.Original, JpegType, CancellationToken.None);
        await using (var thumbnailStream = OpenTestImageForReading(1))
            await _imageBlobAzureRepository.UploadImage(sharedImageName, thumbnailStream, ImageSizeType.Thumbnail, JpegType, CancellationToken.None);

        await _imageBlobAzureRepository.TryDeleteImage(sharedImageName, ImageSizeType.Original, CancellationToken.None);

        Assert.False(await _imageBlobAzureRepository.ImageExists(sharedImageName, ImageSizeType.Original, CancellationToken.None));
        Assert.True(await _imageBlobAzureRepository.ImageExists(sharedImageName, ImageSizeType.Thumbnail, CancellationToken.None));
    }

    /// <summary>
    /// Both containers must be publicly readable, otherwise the urls returned by the api would not
    /// be usable by the clients.
    /// </summary>
    [Theory]
    [InlineData(Constants.AzureBlobStorage.ImagesContainerName)]
    [InlineData(Constants.AzureBlobStorage.ThumbnailsContainerName)]
    public async Task CreateContainerIfNotExists_CreatedContainer_HasPublicBlobAccess(string containerName)
    {
        //make sure the lazily created containers do exist by now
        await UploadTestImage(ImageSizeType.Original);
        await UploadTestImage(ImageSizeType.Thumbnail);

        var containerProperties = await _fixture.BlobServiceClient
            .GetBlobContainerClient(containerName)
            .GetPropertiesAsync();

        Assert.Equal(Azure.Storage.Blobs.Models.PublicAccessType.Blob, containerProperties.Value.PublicAccess);
    }

    private async Task<(string, UploadImageResult)> UploadTestImage(ImageSizeType imageType)
    {
        await using var fileStream = OpenTestImageForReading();
        var imageName = GenerateUniqueImageName();

        var uploadImageResult = await _imageBlobAzureRepository.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);
        return (imageName, uploadImageResult);
    }

    private string GenerateUniqueImageName() => Guid.NewGuid().ToString();

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
