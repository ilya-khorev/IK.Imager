namespace IK.Imager.ImageBlobStorage.AzureFiles
{
    public class ImageAzureStorageConfiguration
    {
        /// <summary>
        /// Connection string to Azure account
        /// Leave null or empty for development mode
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// Container name where all original image files are stored
        /// </summary>
        public string ImagesContainerName { get; set; }
        
        /// <summary>
        /// Container name where thumbnails are stored
        /// </summary>
        public string ThumbnailsContainerName { get; set; }
    }
}