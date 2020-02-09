namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public class ImageMetadataStorageConfiguration
    {
        public string Endpoint { get; set; }
        public string AuthKey { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        public string PartitionKeyPath { get; set; }
        public int ContainerThroughPutOnCreation { get; set; }
    }
}