using System.IO;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using Xunit;

namespace IK.Imager.Core.Tests
{
    public class ImageMetadataTests
    {
        private readonly ImageMetadataReader _imageMetadataReader;
        
        public ImageMetadataTests()
        {
            _imageMetadataReader = new ImageMetadataReader();
        }
        
        [Fact]
        public async Task JpegDetectionTest()
        {
            await DetectionTest(ImageTestsHelper.JpegImagesDirectory, ImageType.JPEG);
        }
        
        [Fact]
        public async Task PngDetectionTest()
        {
            await DetectionTest(ImageTestsHelper.PngImagesDirectory, ImageType.PNG);
        }
        
        [Fact]
        public async Task BmpDetectionTest()
        {
            await DetectionTest(ImageTestsHelper.BmpImagesDirectory, ImageType.BMP);
        }

        [Fact]
        public async Task GifDetectionTest()
        {
            await DetectionTest(ImageTestsHelper.GifImagesDirectory, ImageType.GIF);
        }

        [Fact]
        public async Task ReadImageSizeTest()
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading(Path.Combine(ImageTestsHelper.JpegImagesDirectory, "1043-1200x900.jpg"));
            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Equal(1200, imageSize.Width);
            Assert.Equal(900, imageSize.Height);
            Assert.Equal(265504, imageSize.Bytes);
        }
        
        [Fact]
        public async Task UnsupportedImageFormatTest()
        {
            await using var fileStream = ImageTestsHelper.OpenFileForReading("Images\\556-200x300.webp");
            var imageFormat = _imageMetadataReader.DetectFormat(fileStream);
            Assert.Null(imageFormat);

            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Null(imageSize);
        }
        
        [Fact]
        public async Task NotImageDetectionTest()
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