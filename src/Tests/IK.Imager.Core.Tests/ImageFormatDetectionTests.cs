using System.IO;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;
using Xunit;

namespace IK.Imager.Core.Tests
{
    public class ImageFormatDetectionTests
    {
        private readonly ImageMetadataReader _imageMetadataReader;
        
        public ImageFormatDetectionTests()
        {
            _imageMetadataReader = new ImageMetadataReader();
        }
        
        [Fact]
        public async Task ShouldDetectJpeg()
        {
            await DetectionTest(ImageTestsHelper.JpegImagesDirectory, ImageType.JPEG);
        }
        
        [Fact]
        public async Task ShouldDetectPng()
        {
            await DetectionTest(ImageTestsHelper.PngImagesDirectory, ImageType.PNG);
        }
        
        [Fact]
        public async Task ShouldDetectBmp()
        {
            await DetectionTest(ImageTestsHelper.BmpImagesDirectory, ImageType.BMP);
        }

        [Fact]
        public async Task ShouldDetectGif()
        {
            await DetectionTest(ImageTestsHelper.GifImagesDirectory, ImageType.GIF);
        }

        [Fact]
        public async Task ShouldDetectImageSize()
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading(Path.Combine(ImageTestsHelper.JpegImagesDirectory, "1043-1200x900.jpg"));
            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Equal(1200, imageSize.Width);
            Assert.Equal(900, imageSize.Height);
            Assert.Equal(265504, imageSize.Bytes);
        }
        
        [Fact]
        public async Task ShouldNotDetectUnsupportedFormat()
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading("Images\\556-200x300.webp");
            var imageFormat = _imageMetadataReader.DetectFormat(fileStream);
            Assert.Null(imageFormat);

            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Null(imageSize);
        }
        
        [Fact]
        public async Task ShouldNotDetectText()
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading("textFile.txt");
            var imageFormat = _imageMetadataReader.DetectFormat(fileStream);
            Assert.Null(imageFormat);
            
            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Null(imageSize);
        }
        
        private async Task DetectionTest(string directory, ImageType expectedImageType)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                await using var fileStream = ImageTestsHelper.OpenFileForReading(file);
                var imageFormat = _imageMetadataReader.DetectFormat(fileStream);
                
                Assert.Equal(expectedImageType, imageFormat.ImageType);
            }
        }
    }
}