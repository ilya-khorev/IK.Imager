namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public class ImageMetadataCosmosDbStorageSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseId { get; set; } = null!;
        public string ContainerId { get; set; } = null!;
        public int ContainerThroughPutOnCreation { get; set; }
    }
}