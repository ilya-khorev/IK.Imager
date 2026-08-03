namespace IK.Imager.Core.Abstractions.Thumbnails
{
    public class ImageThumbnailGeneratingResult
    {
        public string Id { get; set; } = null!;
        public int Width { get; set; }
        public int Height { get; set; }
        public string MimeType { get; set; } = null!;
    }
}