namespace IK.Imager.Storage.CosmosDb
{
    public class CosmosDbSettings
    {
        /// <summary>
        /// Connection string to the Cosmos DB account.
        /// Not read when <see cref="AccountEndpoint"/> is set.
        /// </summary>
        public string ConnectionString { get; set; } = null!;

        /// <summary>
        /// Endpoint of the Cosmos DB account, e.g. https://myaccount.documents.azure.com:443/.
        /// Setting it reaches the account with DefaultAzureCredential instead of the connection string.
        /// The database and the container must then already exist - see <see cref="ImageContainerFactory"/>.
        /// </summary>
        public string AccountEndpoint { get; set; } = null!;

        public string DatabaseId { get; set; } = null!;
        public string ContainerId { get; set; } = null!;
        public int ContainerThroughputOnCreation { get; set; }
    }
}
