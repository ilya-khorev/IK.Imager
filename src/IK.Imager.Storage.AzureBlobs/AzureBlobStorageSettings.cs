namespace IK.Imager.Storage.AzureBlobs
{
    public class AzureBlobStorageSettings
    {
        /// <summary>
        /// Connection string to Azure account
        /// Leave null or empty for development mode
        /// </summary>
        public string ConnectionString { get; set; } = null!;

        /// <summary>
        /// Container name where all original image files are stored
        /// </summary>
        public string ImagesContainerName { get; set; } = null!;

        /// <summary>
        /// Container name where thumbnails are stored
        /// </summary>
        public string ThumbnailsContainerName { get; set; } = null!;
    }
}
