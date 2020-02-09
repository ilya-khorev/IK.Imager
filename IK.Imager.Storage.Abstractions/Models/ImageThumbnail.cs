namespace IK.Imager.Storage.Abstractions.Models
{
    public class ImageThumbnail: IImageBasicDetails
    {
        public string Id { get; set; }
        public int Size { get; set; }
        public string MD5Hash { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}