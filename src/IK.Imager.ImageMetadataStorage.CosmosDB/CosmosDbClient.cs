using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public class CosmosDbClient: ICosmosDbClient
    {
        private readonly IOptions<ImageMetadataCosmosDbStorageSettings> _settings;
        private readonly CosmosClient _client;
        private Container _imageContainer;
        
        public CosmosDbClient(IOptions<ImageMetadataCosmosDbStorageSettings> settings)
        {
            _settings = settings;
            _client = new CosmosClient(_settings.Value.ConnectionString);
        }
        
        public async Task<Container> CreateImagesContainerIfNotExists()
        {
            if (_imageContainer != null)
                return _imageContainer;
            
            var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(_settings.Value.DatabaseId);

            ContainerProperties containerProperties = new ContainerProperties(_settings.Value.ContainerId, "/" + nameof(ImageMetadata.ImageGroup));
           
            var indexingPolicy = new IndexingPolicy();
            indexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            //It's unlikely that we will ever request by the following properties, so stop indexing them to save some money
            IgnoreIndexing(indexingPolicy,nameof(ImageMetadata.Thumbnails));
            IgnoreIndexing(indexingPolicy,nameof(ImageMetadata.SizeBytes));
            IgnoreIndexing(indexingPolicy,nameof(ImageMetadata.MD5Hash));
            IgnoreIndexing(indexingPolicy,nameof(ImageMetadata.Width));
            IgnoreIndexing(indexingPolicy,nameof(ImageMetadata.Height));
            IgnoreIndexing(indexingPolicy,nameof(ImageMetadata.MimeType));
            containerProperties.IndexingPolicy = indexingPolicy;
            
            _imageContainer = (await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties,
                throughput: _settings.Value.ContainerThroughPutOnCreation)).Container;
            return _imageContainer;
        }
        
        private void IgnoreIndexing(IndexingPolicy indexingPolicy, string param)
        {
            indexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/" + param + "/*" });
        }
    }
}