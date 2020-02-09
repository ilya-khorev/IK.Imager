using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using Xunit;

namespace IK.Imager.ImageStorage.AzureFiles.Tests
{
    public class ImageAzureStorageTests
    {
        private readonly ImageAzureStorage _imageAzureStorage;

        private const string Image1Path = "Images\\1018-800x800.jpg";
        private const string Image2Path = "Images\\1051-800x800.jpg";

        private const string JpegType = "image/jpeg";

        public ImageAzureStorageTests()
        {
            ImageAzureStorageConfiguration configuration = new ImageAzureStorageConfiguration("images", "thumbnails");
            _imageAzureStorage = new ImageAzureStorage(configuration);
        }

        [Fact]
        public async Task UploadImageTest()
        {
            var imageType = ImageType.Thumbnail;
            using var fileStream = OpenImageForReading(Image1Path);
            var uploadImageResult = await _imageAzureStorage.UploadImage(fileStream, imageType, JpegType, CancellationToken.None);
            Assert.NotNull(uploadImageResult.Id);
            Assert.NotNull(uploadImageResult.MD5Hash);
            
            Assert.True(await _imageAzureStorage.ImageExists(uploadImageResult.Id, imageType, CancellationToken.None));
        }

        [Fact]
        public async Task UploadImageWithGivenIdTest()
        {
            var imageType = ImageType.Original;

            using var fileStream = OpenImageForReading(Image1Path);
            string imageId = Guid.NewGuid().ToString();
            await _imageAzureStorage.UploadImage(imageId, fileStream, imageType, JpegType, CancellationToken.None);

            Assert.True(await _imageAzureStorage.ImageExists(imageId, imageType, CancellationToken.None));
        }

        [Fact]
        public async Task DownloadImageTest()
        {
            var imageType = ImageType.Original;

            using var fileStream = OpenImageForReading(Image2Path);
            using MemoryStream imageStream = new MemoryStream();
            await fileStream.CopyToAsync(imageStream);
            imageStream.Position = 0;

            var uploadImageResult =
                await _imageAzureStorage.UploadImage(imageStream, imageType, JpegType, CancellationToken.None);
            using var downloadedImageStream =
                await _imageAzureStorage.DownloadImage(uploadImageResult.Id, imageType, CancellationToken.None);

            Assert.True(CompareMemoryStreams(imageStream, downloadedImageStream));
        }

        [Fact]
        public async Task ImageDeleteTest()
        {
            var imageType = ImageType.Original;
            using var fileStream = OpenImageForReading(Image2Path);
            var uploadImageResult = await _imageAzureStorage.UploadImage(fileStream, imageType, JpegType, CancellationToken.None);

            Assert.True(await _imageAzureStorage.TryDeleteImage(uploadImageResult.Id, imageType, CancellationToken.None));
            Assert.False(await _imageAzureStorage.ImageExists(uploadImageResult.Id, imageType, CancellationToken.None));

            Assert.False(await _imageAzureStorage.TryDeleteImage(uploadImageResult.Id, imageType, CancellationToken.None));
        }

        [Fact]
        public async Task GetImageUriTest()
        {
            var imageType = ImageType.Original;
            using var fileStream = OpenImageForReading(Image2Path);
            var uploadImageResult = await _imageAzureStorage.UploadImage(fileStream, imageType, JpegType, CancellationToken.None);

            var imageUri = _imageAzureStorage.GetImageUri(uploadImageResult.Id, imageType);

            using HttpClient client = new HttpClient();
            using Stream streamByUri = await client.GetStreamAsync(imageUri);
            using MemoryStream memoryStreamByUri = new MemoryStream();
            await streamByUri.CopyToAsync(memoryStreamByUri);

            using var downloadedImageStream =
                await _imageAzureStorage.DownloadImage(uploadImageResult.Id, imageType, CancellationToken.None);

            Assert.True(CompareMemoryStreams(memoryStreamByUri, downloadedImageStream));
        }

        private FileStream OpenImageForReading(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read);
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