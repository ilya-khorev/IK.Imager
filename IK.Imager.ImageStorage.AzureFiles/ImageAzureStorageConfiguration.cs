namespace IK.Imager.ImageStorage.AzureFiles
{
    public class ImageAzureStorageConfiguration
    {
        public string ConnectionString { get; }
        public string ImagesContainerName { get; }
        public string ThumbnailsContainerName { get; }
    }
}