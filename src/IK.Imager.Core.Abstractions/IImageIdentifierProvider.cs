namespace IK.Imager.Core.Abstractions
{
    public interface IImageIdentifierProvider
    {
        string GenerateUniqueId();
        string GetImageName(string imageId, string extension);
    }
}