using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Abstractions.Models;
using Xunit;

namespace IK.Imager.ImageStorage.AzureFiles.Tests
{
    
    public class ImageAzureStorageTests
    {
        private readonly ImageAzureStorage _imageAzureStorage;
        
        public ImageAzureStorageTests()
        {
            ImageAzureStorageConfiguration configuration = new ImageAzureStorageConfiguration("images", "thumbnails");
            _imageAzureStorage = new ImageAzureStorage(configuration);
        }
        
        [Fact]
        public async Task UploadImageTest()
        {
            var fileStream = File.Open("Images\\1018-800x800.jpg", FileMode.Open, FileAccess.Read);
            var imageId = await _imageAzureStorage.UploadImage(fileStream, ImageType.Thumbnail, CancellationToken.None);
            Assert.NotNull(imageId);
        }
    }
}