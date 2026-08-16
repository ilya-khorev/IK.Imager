namespace IK.Imager.Core.Abstractions
{
    public interface IImageNameGenerator
    {
        string NewImageId();
        string ToFileName(string imageId, string extension);
    }
}
