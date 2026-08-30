using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IK.Imager.Storage.CosmosDb
{
    public class ImageContainerFactory : IImageContainerFactory
    {
        private readonly IOptions<CosmosDbSettings> _settings;
        private readonly CosmosClient _client;
        private readonly bool _provision;
        private Container? _imageContainer;

        /// <param name="client">
        /// The account client. It is registered by the module rather than built here so that the health
        /// check probes the same account, with the same credential, as this factory - and so the
        /// integration tests can substitute a client carrying emulator specific SDK options.
        /// </param>
        /// <param name="settings">Cosmos DB database and container settings.</param>
        /// <param name="provision">
        /// Whether the database and the container are created when they are missing. False for a client
        /// authenticated with a token: Cosmos DB data plane RBAC covers reading and writing documents only,
        /// and creating a database or a container is a control plane operation it cannot perform. Such a
        /// deployment provisions both alongside the account, and this factory then just takes a handle on
        /// what is already there.
        /// </param>
        public ImageContainerFactory(CosmosClient client, IOptions<CosmosDbSettings> settings, bool provision)
        {
            _client = client;
            _settings = settings;
            _provision = provision;
        }

        public async Task<Container> CreateImagesContainerIfNotExists()
        {
            if (_imageContainer != null)
                return _imageContainer;

            if (!_provision)
            {
                //a client side handle - a container that is not there surfaces on the first read
                _imageContainer = _client.GetContainer(_settings.Value.DatabaseId, _settings.Value.ContainerId);
                return _imageContainer;
            }

            var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(_settings.Value.DatabaseId);

            /*
             * A hierarchical partition key: the tenant, then the image id.
             *
             * The second level is what keeps a tenant from ever hitting the 20 GB logical partition
             * limit, and it makes an id unique within its tenant - a logical partition holds exactly
             * one document, so CreateItemAsync returning Conflict means "that id is taken here".
             *
             * Note CreateContainerIfNotExistsAsync matches on the container id alone. Pointing this at
             * a container that already exists with a different partition key silently returns that one,
             * and nothing fails until a read crosses tenants - so change CosmosDb:ContainerId rather
             * than expecting an existing container to be migrated in place.
             */
            ContainerProperties containerProperties = new ContainerProperties(_settings.Value.ContainerId,
                new List<string> { "/" + nameof(ImageMetadata.TenantId), "/id" });

            var indexingPolicy = new IndexingPolicy();
            indexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            //It's unlikely that we will ever request by the following properties, so stop indexing them to save some money
            IgnoreIndexing(indexingPolicy, nameof(ImageMetadata.Thumbnails));
            IgnoreIndexing(indexingPolicy, nameof(ImageMetadata.SizeBytes));
            IgnoreIndexing(indexingPolicy, nameof(ImageMetadata.MD5Hash));
            IgnoreIndexing(indexingPolicy, nameof(ImageMetadata.Width));
            IgnoreIndexing(indexingPolicy, nameof(ImageMetadata.Height));
            IgnoreIndexing(indexingPolicy, nameof(ImageMetadata.MimeType));
            containerProperties.IndexingPolicy = indexingPolicy;

            _imageContainer = (await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties,
                throughput: _settings.Value.ContainerThroughputOnCreation)).Container;
            return _imageContainer;
        }

        private void IgnoreIndexing(IndexingPolicy indexingPolicy, string param)
        {
            indexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/" + param + "/*" });
        }
    }
}
