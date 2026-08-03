using System.IO;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Thumbnails
{
    public class ImageResizingResult
    {
        public MemoryStream Image { get; set; } = null!;
        public ImageSize Size { get; set; } = null!;
    }
}