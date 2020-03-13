using System.IO;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using Xunit;

namespace IK.Imager.Core.Tests
{
    public class ImageDetectionTests
    {
        private const string JpegImagesDirectory = "Images\\jpeg";
        private const string PngImagesDirectory = "Images\\png";
        private const string BmpImagesDirectory = "Images\\bmp";
        
        private readonly ImageFormatDetector _imageFormatDetector;
        
        public ImageDetectionTests()
        {
            _imageFormatDetector = new ImageFormatDetector();
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

        public void GifDetectionTest()
        {
            //todo
        }
        
        public void UnsupportedImageFormatTest()
        {
            //todo
        }
        
        [Fact]
        public async Task NotImageDetectionTest()
        {
            await using var fileStream = OpenFileForReading("textFile.txt");
            var imageFormat = _imageFormatDetector.DetectFormat(fileStream);
            Assert.Null(imageFormat);
        }
        
        private async Task DetectionTest(string directory, ImageType expectedImageType)
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                await using var fileStream = OpenFileForReading(file);
                var imageFormat = _imageFormatDetector.DetectFormat(fileStream);
                
                Assert.Equal(expectedImageType, imageFormat.ImageType);
            }
        }
        
        private FileStream OpenFileForReading(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read);
        }
    }
}