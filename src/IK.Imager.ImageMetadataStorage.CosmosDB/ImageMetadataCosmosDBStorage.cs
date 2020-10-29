using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;
using IK.Imager.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public class ImageMetadataCosmosDbStorage : IImageMetadataStorage
    {
        private readonly IOptions<ImageMetadataCosmosDbStorageSettings> _settings;

        public ImageMetadataCosmosDbStorage(IOptions<ImageMetadataCosmosDbStorageSettings> settings)
        {
            _settings = settings;
        }

        public async Task SetMetadata(ImageMetadata metadata, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNull(nameof(metadata), metadata);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.Id), metadata.Id);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.PartitionKey), metadata.PartitionKey);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.MimeType), metadata.MimeType);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.MD5Hash), metadata.MD5Hash);
            if (metadata.SizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.SizeBytes));
            if (metadata.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.Width));
            if (metadata.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.Height));

            var container = await GetContainer();

            await container.UpsertItemAsync(metadata, new PartitionKey(metadata.PartitionKey), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        /*
         * Using of partitions make search requests more efficient
         * https://docs.microsoft.com/en-us/azure/cosmos-db/partitioning-overview
         */
        
        public async Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string partitionKey,
            CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNull(nameof(imageIds), imageIds);
            if (imageIds.Count < 1)
                throw new ArgumentException("Please provide at least one image id");

            var container = await GetContainer();

            QueryRequestOptions queryRequestOptions = null;
            if (!string.IsNullOrEmpty(partitionKey))
                queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(partitionKey)
                };

            var queryIterator = container
                .GetItemLinqQueryable<ImageMetadata>(requestOptions: queryRequestOptions)
                .Where(x => imageIds.Contains(x.Id))
                .ToFeedIterator();

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

        public async Task<bool> RemoveMetadata(string imageId, string partitionKey, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageId), imageId);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(partitionKey), partitionKey);

            var container = await GetContainer();
            ItemResponse<ImageMetadata> response;
            try
            {
                response = await container.DeleteItemAsync<ImageMetadata>(imageId, new PartitionKey(partitionKey), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return false;

                throw;
            }
    
            return response.StatusCode == HttpStatusCode.NoContent;
        }

        //todo add operation to search by tags

        /// <summary>
        /// Use GetContainer method instead
        /// </summary>
        private Container _containerInternal;

        private async Task<Container> GetContainer()
        {
            if (_containerInternal != null)
                return _containerInternal;

            CosmosClient client = new CosmosClient(_settings.Value.ConnectionString);
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(_settings.Value.DatabaseId);

            ContainerProperties containerProperties = new ContainerProperties(_settings.Value.ContainerId, "/" + nameof(ImageMetadata.PartitionKey));
           
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
            
            _containerInternal = (await databaseResponse.Database.CreateContainerIfNotExistsAsync(containerProperties,
                throughput: _settings.Value.ContainerThroughPutOnCreation)).Container;

            return _containerInternal;
        }

        private void IgnoreIndexing(IndexingPolicy indexingPolicy, string param)
        {
            indexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/" + param + "/*" });
        }
    }
}