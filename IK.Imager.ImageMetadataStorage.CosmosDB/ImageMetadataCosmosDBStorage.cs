using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public class ImageMetadataCosmosDbStorage : IImageMetadataStorage
    {
        private readonly ImageMetadataCosmosDbStorageConfiguration _configuration;

        public ImageMetadataCosmosDbStorage(ImageMetadataCosmosDbStorageConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task SetMetadata(ImageMetadata metadata, CancellationToken cancellationToken)
        {
            var container = await GetContainer();

            await container.UpsertItemAsync(metadata, new PartitionKey(metadata.PartitionKey),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string partitionKey,
            CancellationToken cancellationToken)
        {
            var container = await GetContainer();
            
            QueryRequestOptions queryRequestOptions = null;
            if (!string.IsNullOrEmpty(partitionKey))
                queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(partitionKey)
                };

            var queryIterator = container.GetItemLinqQueryable<ImageMetadata>(requestOptions: queryRequestOptions)
                .Where(x => imageIds.Contains(x.Id) && !x.ToRemove).ToFeedIterator();

            List<ImageMetadata> result = new List<ImageMetadata>();
            while (queryIterator.HasMoreResults)
            {
                FeedResponse<ImageMetadata> response = await queryIterator.ReadNextAsync(cancellationToken);
                result.AddRange(response);
            }

            return result;
        }

        public Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, CancellationToken cancellationToken)
        {
            return GetMetadata(imageIds, null, cancellationToken);
        }

        public async Task<bool> SetMetadataForRemoval(string imageId, string partitionKey,
            CancellationToken cancellationToken)
        {
            var container = await GetContainer();

            var response = await container.ReadItemAsync<ImageMetadata>(imageId, new PartitionKey(partitionKey),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (response.Resource == null)
                return false;

            response.Resource.ToRemove = true;
            var updateResponse = await container.UpsertItemAsync(imageId, new PartitionKey(partitionKey),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return updateResponse.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> RemoveMetadata(string imageId, string partitionKey, CancellationToken cancellationToken)
        {
            var container = await GetContainer();
            var response = await container.DeleteItemAsync<ImageMetadata>(imageId, new PartitionKey(partitionKey),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.StatusCode == HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Use GetContainer method instead
        /// </summary>
        private Container _containerInternal;

        private async Task<Container> GetContainer()
        {
            if (_containerInternal != null)
                return _containerInternal;

            CosmosClient client = new CosmosClient(_configuration.Endpoint, _configuration.AuthKey);
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(_configuration.DatabaseId);

            ContainerProperties containerProperties =
                new ContainerProperties(_configuration.ContainerId, _configuration.PartitionKeyPath);

            _containerInternal = (await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties,
                throughput: _configuration.ContainerThroughPutOnCreation)).Container;

            return _containerInternal;
        }
    }
}