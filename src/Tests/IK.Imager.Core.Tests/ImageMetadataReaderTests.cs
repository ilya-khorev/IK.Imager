using System;
using System.IO;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using Xunit;

namespace IK.Imager.Core.Tests
{
    public class ImageMetadataReaderTests
    {
        private readonly ImageMetadataReader _imageMetadataReader;

        public ImageMetadataReaderTests()
        {
            _imageMetadataReader = new ImageMetadataReader();
        }

        [Theory]
        [InlineData(ImageTestsHelper.JpegImagesDirectory, ImageType.JPEG)]
        [InlineData(ImageTestsHelper.PngImagesDirectory, ImageType.PNG)]
        [InlineData(ImageTestsHelper.BmpImagesDirectory, ImageType.BMP)]
        [InlineData(ImageTestsHelper.GifImagesDirectory, ImageType.GIF)]
        public async Task DetectFormat_SupportedFormat_ReturnsFormatModel(string imageDirectory, ImageType expectedType)
        {
            foreach (var file in Directory.EnumerateFiles(imageDirectory))
            {
                await using var fileStream = ImageTestsHelper.OpenFileForReading(file);
                var imageFormat = _imageMetadataReader.DetectFormat(fileStream);

                Assert.Equal(expectedType, imageFormat.ImageType);
            }
        }

        [Theory]
        [InlineData(ImageTestsHelper.WebpImagePath)]
        public async Task DetectFormat_UnsupportedImageFormat_NotSupportedException(string filePath)
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading(filePath);
            Assert.Throws<NotSupportedException>(() => _imageMetadataReader.DetectFormat(fileStream));
        }
        
        [Theory]
        [InlineData(ImageTestsHelper.TextFilePath)]
        public async Task DetectFormat_UnrecognizedFormat_ReturnsNull(string filePath)
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading(filePath);
            var format = _imageMetadataReader.DetectFormat(fileStream);
            Assert.Null(format);
        }
        
        [Theory]
        [InlineData(ImageTestsHelper.JpegImagesDirectory + "\\1043-1200x900.jpg", 1200, 900, 265504)]
        [InlineData(ImageTestsHelper.BmpImagesDirectory + "\\1068-1000x2000.bmp", 1000, 2000, 8000138)]
        [InlineData(ImageTestsHelper.GifImagesDirectory + "\\giphy_400x400.gif", 400, 400, 149130)]
        [InlineData(ImageTestsHelper.PngImagesDirectory + "\\1060-800x800.png", 800, 800, 514792)]
        public async Task ReadSize_SupportedFormat_ReturnsSizeModel(string imagePath, int expectedWidth,
            int expectedHeight, int expectedSize)
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading(imagePath);
            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Equal(expectedWidth, imageSize.Width);
            Assert.Equal(expectedHeight, imageSize.Height);
            Assert.Equal(expectedSize, imageSize.Bytes);
        }

        [Theory]
        [InlineData(ImageTestsHelper.TextFilePath)]
        public async Task ReadSize_UnsupportedFormat_ReturnsNull(string filePath)
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading(filePath);
            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Null(imageSize);
        }
    }
}