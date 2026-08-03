namespace IK.Imager.Core.Abstractions.Models
{
    public class ImageShortInfo
    {
        public string ImageId { get; set; } = null!;
        public string ImageName { get; set; } = null!;
        public string[] ThumbnailNames { get; set; } = [];

        public override string ToString()
        {
            return $"ImageId = {ImageId}, ImageName = {ImageName}, ThumbnailNames = {string.Join(",", ThumbnailNames)}";
        }
    }
}