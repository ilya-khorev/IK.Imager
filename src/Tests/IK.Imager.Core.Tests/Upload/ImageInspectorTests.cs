using System;
using System.IO;
using System.Threading.Tasks;
using IK.Imager.Core.Tests.Infrastructure;
using IK.Imager.Core.Upload;
using IK.Imager.Storage.Abstractions.Models;
using Xunit;

namespace IK.Imager.Core.Tests.Upload
{
    public class ImageInspectorTests
    {
        private readonly ImageInspector _imageInspector;

        public ImageInspectorTests()
        {
            _imageInspector = new ImageInspector();
        }

        [Theory]
        [InlineData(SampleImages.JpegImagesDirectory, ImageType.JPEG)]
        [InlineData(SampleImages.PngImagesDirectory, ImageType.PNG)]
        [InlineData(SampleImages.BmpImagesDirectory, ImageType.BMP)]
        [InlineData(SampleImages.GifImagesDirectory, ImageType.GIF)]
        public async Task DetectFormat_SupportedFormat_ReturnsFormatModel(string imageDirectory, ImageType expectedType)
        {
            foreach (var file in Directory.EnumerateFiles(imageDirectory))
            {
                await using var fileStream = SampleImages.OpenFileForReading(file);
                var imageFormat = _imageInspector.DetectFormat(fileStream);

                Assert.NotNull(imageFormat);
                Assert.Equal(expectedType, imageFormat.ImageType);
            }
        }

        [Theory]
        [InlineData(SampleImages.TgaImagePath)]
        public async Task DetectFormat_UnsupportedImageFormat_ThrowsNotSupportedException(string filePath)
        {
            await using var fileStream = SampleImages.OpenFileForReading(filePath);
            Assert.Throws<NotSupportedException>(() => _imageInspector.DetectFormat(fileStream));
        }

        [Theory]
        [InlineData(SampleImages.TextFilePath)]
        public async Task DetectFormat_UnrecognizedFormat_ReturnsNull(string filePath)
        {
            await using var fileStream = SampleImages.OpenFileForReading(filePath);
            var format = _imageInspector.DetectFormat(fileStream);
            Assert.Null(format);
        }

        [Theory]
        [InlineData(SampleImages.JpegImagesDirectory + "/1043-1200x900.jpg", 1200, 900, 265504)]
        [InlineData(SampleImages.BmpImagesDirectory + "/1068-1000x2000.bmp", 1000, 2000, 8000138)]
        [InlineData(SampleImages.GifImagesDirectory + "/giphy_400x400.gif", 400, 400, 149130)]
        [InlineData(SampleImages.PngImagesDirectory + "/1060-800x800.png", 800, 800, 514792)]
        [InlineData(SampleImages.WebpImagePath, 200, 300, 3086)]
        public async Task ReadSize_SupportedFormat_ReturnsSizeModel(string imagePath, int expectedWidth,
            int expectedHeight, int expectedSize)
        {
            await using var fileStream = SampleImages.OpenFileForReading(imagePath);
            var imageSize = _imageInspector.ReadSize(fileStream);
            Assert.NotNull(imageSize);
            Assert.Equal(expectedWidth, imageSize.Width);
            Assert.Equal(expectedHeight, imageSize.Height);
            Assert.Equal(expectedSize, imageSize.Bytes);
        }

        [Theory]
        [InlineData(SampleImages.TextFilePath)]
        public async Task ReadSize_UnsupportedFormat_ReturnsNull(string filePath)
        {
            await using var fileStream = SampleImages.OpenFileForReading(filePath);
            var imageSize = _imageInspector.ReadSize(fileStream);
            Assert.Null(imageSize);
        }
    }
}
