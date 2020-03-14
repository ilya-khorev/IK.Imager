using System.IO;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using Xunit;

namespace IK.Imager.Core.Tests
{
    public class ImageMetadataTests
    {
        private const string JpegImagesDirectory = "Images\\jpeg";
        private const string PngImagesDirectory = "Images\\png";
        private const string BmpImagesDirectory = "Images\\bmp";
        private const string GifImagesDirectory = "Images\\gif";
        
        private readonly ImageMetadataReader _imageMetadataReader;
        
        public ImageMetadataTests()
        {
            _imageMetadataReader = new ImageMetadataReader();
        }
        
        [Fact]
        public async Task JpegDetectionTest()
        {
            await DetectionTest(JpegImagesDirectory, ImageType.JPEG);
        }
        
        [Fact]
        public async Task PngDetectionTest()
        {
            await DetectionTest(PngImagesDirectory, ImageType.PNG);
        }
        
        [Fact]
        public async Task BmpDetectionTest()
        {
            await DetectionTest(BmpImagesDirectory, ImageType.BMP);
        }

        [Fact]
        public async Task GifDetectionTest()
        {
            await DetectionTest(GifImagesDirectory, ImageType.GIF);
        }

        [Fact]
        public async Task ReadImageSizeTest()
        {
            await using var fileStream = OpenFileForReading(Path.Combine(JpegImagesDirectory, "1043-1200x900.jpg"));
            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Equal(1200, imageSize.Width);
            Assert.Equal(900, imageSize.Height);
            Assert.Equal(265504, imageSize.Bytes);
        }
        
        [Fact]
        public async Task UnsupportedImageFormatTest()
        {
            await using var fileStream = OpenFileForReading("Images\\556-200x300.webp");
            var imageFormat = _imageMetadataReader.DetectFormat(fileStream);
            Assert.Null(imageFormat);

            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Null(imageSize);
        }
        
        [Fact]
        public async Task NotImageDetectionTest()
        {
            await using var fileStream = OpenFileForReading("textFile.txt");
            var imageFormat = _imageMetadataReader.DetectFormat(fileStream);
            Assert.Null(imageFormat);
            
            var imageSize = _imageMetadataReader.ReadSize(fileStream);
            Assert.Null(imageSize);
        }
        
        private async Task DetectionTest(string directory, ImageType expectedImageType)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                await using var fileStream = OpenFileForReading(file);
                var imageFormat = _imageMetadataReader.DetectFormat(fileStream);
                
                Assert.Equal(expectedImageType, imageFormat.ImageType);
            }
        }
        
        private FileStream OpenFileForReading(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read);
        }
    }
}