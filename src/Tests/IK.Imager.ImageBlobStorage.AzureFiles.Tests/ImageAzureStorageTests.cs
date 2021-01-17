using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.TestsBase;
using Microsoft.Extensions.Options;
using Xunit;

namespace IK.Imager.ImageStorage.AzureFiles.Tests
{
    public class ImageAzureStorageTests
    {
        private readonly ImageBlobAzureStorage _imageBlobAzureStorage;
        private readonly Random _random;

        private const string TestImagesFolder = "Images";
        private const string JpegType = "image/jpeg";
        
        public ImageAzureStorageTests()
        {
            ImageAzureStorageSettings settings =
                new ImageAzureStorageSettings
                {
                    ConnectionString = Constants.AzureBlobStorage.ConnectionString,
                    ImagesContainerName = Constants.AzureBlobStorage.ImagesContainerName,
                    ThumbnailsContainerName = Constants.AzureBlobStorage.ThumbnailsContainerName
                };
            _imageBlobAzureStorage = new ImageBlobAzureStorage(new OptionsWrapper<ImageAzureStorageSettings>(settings));
            _random = new Random();
        }

        [Theory]
        [InlineData(ImageSizeType.Original)]
        [InlineData(ImageSizeType.Thumbnail)]
        public async Task UploadImage_TestImage_ReturnsUrlAndHash(ImageSizeType imageType)
        {
            await using var fileStream = OpenTestImageForReading();
            string imageName = GenerateUniqueImageName();
            
            var uploadImageResult = await _imageBlobAzureStorage.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);
            
            Assert.NotNull(uploadImageResult.Url);
            Assert.NotNull(uploadImageResult.MD5Hash);

            Assert.True(await _imageBlobAzureStorage.ImageExists(imageName, imageType, CancellationToken.None));
        }
        
        [Theory]
        [InlineData(ImageSizeType.Original)]
        [InlineData(ImageSizeType.Thumbnail)]
        public async Task DownloadImage_TestImage_ReturnsCorrectStream(ImageSizeType imageType)
        {
            await using var fileStream = OpenTestImageForReading();
            await using MemoryStream imageStream = new MemoryStream();
            await fileStream.CopyToAsync(imageStream);
            imageStream.Position = 0;
            string imageName = GenerateUniqueImageName();
            await _imageBlobAzureStorage.UploadImage(imageName, imageStream, imageType, JpegType, CancellationToken.None);
            
            await using var downloadedImageStream = await _imageBlobAzureStorage.DownloadImage(imageName, imageType, CancellationToken.None);

            Assert.True(CompareMemoryStreams(imageStream, downloadedImageStream));
        }

        [Theory]
        [InlineData(ImageSizeType.Original)]
        [InlineData(ImageSizeType.Thumbnail)]
        public async Task ImageDeleteTest(ImageSizeType imageType)
        {
            await using var fileStream = OpenTestImageForReading();
            string imageName = GenerateUniqueImageName();
            await _imageBlobAzureStorage.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);

            Assert.True(await _imageBlobAzureStorage.TryDeleteImage(imageName, imageType, CancellationToken.None));
            
            Assert.False(await _imageBlobAzureStorage.ImageExists(imageName, imageType, CancellationToken.None));
            Assert.False(await _imageBlobAzureStorage.TryDeleteImage(imageName, imageType, CancellationToken.None));
        }

        [Theory]
        [InlineData(ImageSizeType.Original)]
        [InlineData(ImageSizeType.Thumbnail)]
        public async Task GetImageUriTest(ImageSizeType imageType)
        {
            await using var fileStream = OpenTestImageForReading();
            string imageName = GenerateUniqueImageName();
            await _imageBlobAzureStorage.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);

            var imageUri = _imageBlobAzureStorage.GetImageUri(imageName, imageType);

            using HttpClient client = new HttpClient();
            await using Stream streamByUri = await client.GetStreamAsync(imageUri);
            await using MemoryStream memoryStreamByUri = new MemoryStream();
            await streamByUri.CopyToAsync(memoryStreamByUri);

            await using var downloadedImageStream = await _imageBlobAzureStorage.DownloadImage(imageName, imageType, CancellationToken.None);

            Assert.True(CompareMemoryStreams(memoryStreamByUri, downloadedImageStream));
        }

        private string GenerateUniqueImageName() => Guid.NewGuid().ToString();
        
        private FileStream OpenTestImageForReading()
        {
            var testImages = Directory.GetFiles(TestImagesFolder);
            var randomImage = testImages[_random.Next(0, testImages.Length - 1)];
            return File.Open(randomImage, FileMode.Open, FileAccess.Read);
        }

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
}