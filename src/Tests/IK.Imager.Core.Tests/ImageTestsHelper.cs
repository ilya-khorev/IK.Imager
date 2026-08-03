using System.IO;

namespace IK.Imager.Core.Tests;

public static class ImageTestsHelper
{
    //Forward slashes on purpose: these are const so that they can be used in [InlineData], which
    //rules out Path.Combine, and Windows accepts '/' just as happily as Linux does.
    public const string ImagesDirectory = "Images";
    public const string JpegImagesDirectory = ImagesDirectory + "/jpeg";
    public const string PngImagesDirectory = ImagesDirectory + "/png";
    public const string BmpImagesDirectory = ImagesDirectory + "/bmp";
    public const string GifImagesDirectory = ImagesDirectory + "/gif";

    public const string WebpImagePath = ImagesDirectory + "/556-200x300.webp";
    public const string TgaImagePath = ImagesDirectory + "/sample_640×426.tga";

    public const string TextFilePath = "textFile.txt";
        
    public static FileStream OpenFileForReading(string filePath)
    {
        return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
}