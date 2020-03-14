using System.IO;

namespace IK.Imager.Core.Tests
{
    public static class ImageTestsHelper
    {
        public const string JpegImagesDirectory = "Images\\jpeg";
        public const string PngImagesDirectory = "Images\\png";
        public const string BmpImagesDirectory = "Images\\bmp";
        public const string GifImagesDirectory = "Images\\gif";
        
        public static FileStream OpenFileForReading(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read);
        }
    }
}