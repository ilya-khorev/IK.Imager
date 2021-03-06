using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.TestsBase;
using Xunit;

namespace IK.Imager.ImageStorage.AzureFiles.Tests
{
    public class ImageAzureStorageTests
    {
        private readonly ImageBlobAzureStorage _imageBlobAzureStorage;

        private const string Image1Path = "Images\\1018-800x800.jpg";
        private const string Image2Path = "Images\\1051-800x800.jpg";

        private const string JpegType = "image/jpeg";

        public ImageAzureStorageTests()
        {
            ImageAzureStorageConfiguration configuration =
                new ImageAzureStorageConfiguration
                {
                    ConnectionString = Constants.AzureConnectionString,
                    ImagesContainerName = Constants.ImagesContainerName,
                    ThumbnailsContainerName = Constants.ThumbnailsContainerName
                };
            _imageBlobAzureStorage = new ImageBlobAzureStorage(configuration);
        }

        [Fact]
        public async Task UploadImageTest()
        {
            var imageType = ImageSizeType.Thumbnail;
            await using var fileStream = OpenImageForReading(Image1Path);
            string imageName = Guid.NewGuid().ToString();
            var uploadImageResult = await _imageBlobAzureStorage.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);
            Assert.NotNull(uploadImageResult.MD5Hash);
            
            Assert.True(await _imageBlobAzureStorage.ImageExists(imageName, imageType, CancellationToken.None));
        }
        
        [Fact]
        public async Task DownloadImageTest()
        {
            var imageType = ImageSizeType.Original;

            await using var fileStream = OpenImageForReading(Image2Path);
            await using MemoryStream imageStream = new MemoryStream();
            await fileStream.CopyToAsync(imageStream);
            imageStream.Position = 0;
            string imageName = Guid.NewGuid().ToString();

            await _imageBlobAzureStorage.UploadImage(imageName, imageStream, imageType, JpegType, CancellationToken.None);
            await using var downloadedImageStream = await _imageBlobAzureStorage.DownloadImage(imageName, imageType, CancellationToken.None);

            Assert.True(CompareMemoryStreams(imageStream, downloadedImageStream));
        }

        [Fact]
        public async Task ImageDeleteTest()
        {
            var imageType = ImageSizeType.Original;
            await using var fileStream = OpenImageForReading(Image2Path);
            string imageName = Guid.NewGuid().ToString();
            await _imageBlobAzureStorage.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);

            Assert.True(await _imageBlobAzureStorage.TryDeleteImage(imageName, imageType, CancellationToken.None));
            Assert.False(await _imageBlobAzureStorage.ImageExists(imageName, imageType, CancellationToken.None));

            Assert.False(await _imageBlobAzureStorage.TryDeleteImage(imageName, imageType, CancellationToken.None));
        }

        [Fact]
        public async Task GetImageUriTest()
        {
            var imageType = ImageSizeType.Original;
            await using var fileStream = OpenImageForReading(Image2Path);
            string imageName = Guid.NewGuid().ToString();
            await _imageBlobAzureStorage.UploadImage(imageName, fileStream, imageType, JpegType, CancellationToken.None);

            var imageUri = _imageBlobAzureStorage.GetImageUri(imageName, imageType);

            using HttpClient client = new HttpClient();
            await using Stream streamByUri = await client.GetStreamAsync(imageUri);
            await using MemoryStream memoryStreamByUri = new MemoryStream();
            await streamByUri.CopyToAsync(memoryStreamByUri);

            await using var downloadedImageStream = await _imageBlobAzureStorage.DownloadImage(imageName, imageType, CancellationToken.None);

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