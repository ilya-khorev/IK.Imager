namespace IK.Imager.ImageStorage.AzureFiles
{
    public class ImageAzureStorageConfiguration
    {
        /// <summary>
        /// Creates an instance with a given set of settings
        /// </summary>
        /// <param name="connectionString">Connection string to Azure account</param>
        /// <param name="imagesContainerName">Container name where all original image files are stored</param>
        /// <param name="thumbnailsContainerName">Container name where thumbnails are stored</param>
        public ImageAzureStorageConfiguration(string connectionString, string imagesContainerName, string thumbnailsContainerName)
        {
            ConnectionString = connectionString;
            ImagesContainerName = imagesContainerName;
            ThumbnailsContainerName = thumbnailsContainerName;
        }

        /// <summary>
        /// Use for development mode only when connection string is not needed
        /// </summary>
        /// <param name="imagesContainerName">Container name where all original image files are stored</param>
        /// <param name="thumbnailsContainerName">Container name where thumbnails are stored</param>
        public ImageAzureStorageConfiguration(string imagesContainerName, string thumbnailsContainerName)
        {
            ImagesContainerName = imagesContainerName;
            ThumbnailsContainerName = thumbnailsContainerName;
        }
        
        /// <summary>
        /// Connection string to Azure account
        /// Leave null or empty for development mode
        /// </summary>
        public string ConnectionString { get; }
        
        /// <summary>
        /// Container name where all original image files are stored
        /// </summary>
        public string ImagesContainerName { get; }
        
        /// <summary>
        /// Container name where thumbnails are stored
        /// </summary>
        public string ThumbnailsContainerName { get; }
    }
}