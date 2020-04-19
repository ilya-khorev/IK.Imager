namespace IK.Imager.Core.Abstractions.Models
{
    public class ImageShortInfo
    {
        public string ImageId { get; set; }
        public string ImageName { get; set; }
        public string[] ThumbnailNames { get; set; }

        public override string ToString()
        {
            return $"ImageId = {ImageId}, ImageName = {ImageName}, ThumbnailNames = {string.Join(",", ThumbnailNames)}";
        }
    }
}