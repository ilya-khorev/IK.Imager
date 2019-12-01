using System.Collections.Generic;

namespace IK.Imager.Abstractions.Models
{
    public class ImageMetadata: IImageBasicDetails
    {
        public string Id { get; set; }
        public int Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
     
        public string Name { get; set; }
        
        public string Format { get; set; } //todo enum?
        
        public IDictionary<string, string> Tags { get; set; }
        
        public ImageThumbnail[] Thumbnails { get; set; }
    }
}