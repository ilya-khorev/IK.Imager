using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using IK.Imager.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace IK.Imager.ImageMetadataStorage.CosmosDB
{
    public class ImageMetadataCosmosDbRepository : IImageMetadataRepository
    {
        private readonly ICosmosDbClient _cosmosDbClient;

        public ImageMetadataCosmosDbRepository(ICosmosDbClient cosmosDbClient)
        {
            _cosmosDbClient = cosmosDbClient;
        }

        /// <inheritdoc />
        public async Task SetMetadata(ImageMetadata metadata, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNull(nameof(metadata), metadata);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.Id), metadata.Id);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.ImageGroup), metadata.ImageGroup);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.MimeType), metadata.MimeType);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(metadata.MD5Hash), metadata.MD5Hash);
            if (metadata.SizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.SizeBytes));
            if (metadata.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.Width));
            if (metadata.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.Height));

            var container = await _cosmosDbClient.CreateImagesContainerIfNotExists();

            await container.UpsertItemAsync(metadata, new PartitionKey(metadata.ImageGroup), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        /*
         * Using of partitions make search requests more efficient
         * https://docs.microsoft.com/en-us/azure/cosmos-db/partitioning-overview
         */
        
        /// <inheritdoc />
        public async Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string imageGroup,
            CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNull(nameof(imageIds), imageIds);
            if (imageIds.Count < 1)
                throw new ArgumentException("Please provide at least one image id");

            var container = await _cosmosDbClient.CreateImagesContainerIfNotExists();

            QueryRequestOptions queryRequestOptions = null;
            if (!string.IsNullOrEmpty(imageGroup))
                queryRequestOptions = new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(imageGroup)
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

        /// <inheritdoc />
        public Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, CancellationToken cancellationToken)
        {
            return GetMetadata(imageIds, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveMetadata(string imageId, string imageGroup, CancellationToken cancellationToken)
        {
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageId), imageId);
            ArgumentHelper.AssertNotNullOrEmpty(nameof(imageGroup), imageGroup);

            var container = await _cosmosDbClient.CreateImagesContainerIfNotExists();
            ItemResponse<ImageMetadata> response;
            try
            {
                response = await container.DeleteItemAsync<ImageMetadata>(imageId, new PartitionKey(imageGroup), cancellationToken: cancellationToken)
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
    }
}