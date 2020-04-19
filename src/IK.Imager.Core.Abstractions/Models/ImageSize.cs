namespace IK.Imager.Core.Abstractions.Models
{
    public class ImageSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long Bytes { get; set; }

        public override string ToString()
        {
            return $"Width:{Width}, Height:{Height}, Bytes:{Bytes}";
        }
    }
}