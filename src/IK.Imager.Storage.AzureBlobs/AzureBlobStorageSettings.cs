namespace IK.Imager.Storage.AzureBlobs
{
    public class AzureBlobStorageSettings
    {
        /// <summary>
        /// Connection string to Azure account.
        /// Not read when <see cref="ServiceUri"/> is set.
        /// </summary>
        public string ConnectionString { get; set; } = null!;

        /// <summary>
        /// Blob service endpoint of the storage account, e.g. https://myaccount.blob.core.windows.net.
        /// Setting it reaches the account with DefaultAzureCredential instead of the connection string.
        /// </summary>
        public string ServiceUri { get; set; } = null!;

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
