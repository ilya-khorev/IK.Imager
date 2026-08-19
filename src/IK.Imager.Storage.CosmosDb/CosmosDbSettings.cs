namespace IK.Imager.Storage.CosmosDb
{
    public class CosmosDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseId { get; set; } = null!;
        public string ContainerId { get; set; } = null!;
        public int ContainerThroughputOnCreation { get; set; }
    }
}
