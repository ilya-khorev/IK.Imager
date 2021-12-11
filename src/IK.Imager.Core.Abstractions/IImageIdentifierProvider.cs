namespace IK.Imager.Core.Abstractions
{
    public interface IImageIdentifierProvider
    {
        string GenerateUniqueId();
        string GetImageFileName(string imageId, string extension);
    }
}