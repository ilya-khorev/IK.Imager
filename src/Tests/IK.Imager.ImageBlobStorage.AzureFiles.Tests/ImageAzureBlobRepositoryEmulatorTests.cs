using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.Storage.Abstractions.Models;
using Xunit;

namespace IK.Imager.ImageStorage.AzureFiles.Tests
{
    // These tests require Azure Storage Emulator to be installed and launched 
    // https://docs.microsoft.com/en-gb/azure/storage/common/storage-use-emulator
    // 
    // Naming convention:
    // - The name of the method being tested
    // - The scenario under which it's being tested (optional)
    // - The expected behavior when the scenario is invoked
    public class ImageAzureBlobRepositoryEmulatorTests : IClassFixture<StorageFixture>
    {
        private readonly ImageBlobAzureRepository _imageBlobAzureRepository;
        private readonly Random _random;

        private const string TestImagesFolder = "Images";
        private const string JpegType = "image/jpeg";
        
        public ImageAzureBlobRepositoryEmulatorTests(StorageFixture fixture)
        {
            _imageBlobAzureRepository = fixture.BlobImageRepository;
            _random = new Random();
        }

        [Theory]
        [InlineData(ImageSizeType.Original)]
        [InlineData(ImageSizeType.Thumbnail)]
        public async Task UploadImage_ReturnsUrlAndHash(ImageSizeType imageType)
        {
            var uploadImageResult = await UploadTestImage(imageType);
            
            Assert.NotNull(uploadImageResult.Item2.Url);
            Assert.NotNull(uploadImageResult.Item2.MD5Hash);
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

            Assert.True(CompareMemoryStreams(imageStream, downloadedImageStream));
        }
        
        [Theory]
        [InlineData(ImageSizeType.Original)]
        [InlineData(ImageSizeType.Thumbnail)]
        public async Task GetImageUri_ReturnsCorrectUri(ImageSizeType imageType)
        {
            await using var fileStream = OpenTestImageForReading();
            string imageName = GenerateUniqueImageName();
            await _imageBlobAzureRepository.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);

            var imageUri = _imageBlobAzureRepository.GetImageUri(imageName, imageType);
   
            using HttpClient client = new HttpClient();
            await using Stream streamByUri = await client.GetStreamAsync(imageUri);
            await using MemoryStream memoryStreamByUri = new MemoryStream();
            await streamByUri.CopyToAsync(memoryStreamByUri);
            
            Assert.Equal(memoryStreamByUri.Length, fileStream.Length);
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
        
        private async Task<(string, UploadImageResult)> UploadTestImage(ImageSizeType imageType)
        {
            await using var fileStream = OpenTestImageForReading();
            var imageName = GenerateUniqueImageName();

            var uploadImageResult = await _imageBlobAzureRepository.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);
            return (imageName, uploadImageResult);
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