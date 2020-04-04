using System.IO;

namespace IK.Imager.Core.Abstractions
{
    public class ImageResizingResult
    {
        public MemoryStream Image { get; set; }
        public ImageSize Size { get; set; }
    }
}