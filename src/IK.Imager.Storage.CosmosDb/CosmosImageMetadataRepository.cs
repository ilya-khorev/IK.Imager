using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Storage.CosmosDb
{
    public class CosmosImageMetadataRepository : IImageMetadataRepository
    {
        private readonly IImageContainerFactory _cosmosDbClient;
        private readonly ILogger<CosmosImageMetadataRepository> _logger;

        public CosmosImageMetadataRepository(IImageContainerFactory cosmosDbClient,
            ILogger<CosmosImageMetadataRepository> logger)
        {
            _cosmosDbClient = cosmosDbClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task CreateMetadata(ImageMetadata metadata, CancellationToken cancellationToken)
        {
            Validate(metadata);

            var container = await _cosmosDbClient.CreateImagesContainerIfNotExists();

            try
            {
                var response = await container.CreateItemAsync(metadata, PartitionKeyOf(metadata.TenantId, metadata.Id),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                _logger.MetadataCreated(metadata.Id, response.RequestCharge);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                //an id is unique within its logical partition, and the partition is (tenant, id) -
                //so this is exactly "that id is taken in that tenant"
                throw new ImageAlreadyExistsException(metadata.TenantId, metadata.Id, ex);
            }
        }

        /// <inheritdoc />
        public async Task UpdateMetadata(ImageMetadata metadata, CancellationToken cancellationToken)
        {
            Validate(metadata);

            var container = await _cosmosDbClient.CreateImagesContainerIfNotExists();

            var response = await container.UpsertItemAsync(metadata, PartitionKeyOf(metadata.TenantId, metadata.Id),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.MetadataUpdated(metadata.Id, response.RequestCharge);
        }

        /*
         * The partition key is hierarchical - /TenantId then /id - so every read, write and delete here
         * is a point operation on a single logical partition. ReadManyItemsAsync batches the lookup by
         * physical partition, which is cheaper than a query fanning out across all of them.
         * https://learn.microsoft.com/azure/cosmos-db/hierarchical-partition-keys
         */

        /// <inheritdoc />
        public async Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string tenantId,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(imageIds);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);
            if (imageIds.Count < 1)
                throw new ArgumentException("Please provide at least one image id");

            var container = await _cosmosDbClient.CreateImagesContainerIfNotExists();

            var items = new List<(string, PartitionKey)>(imageIds.Count);
            foreach (var imageId in imageIds)
                items.Add((imageId, PartitionKeyOf(tenantId, imageId)));

            var response = await container.ReadManyItemsAsync<ImageMetadata>(items, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var result = new List<ImageMetadata>(response);

            _logger.MetadataRead(result.Count, imageIds.Count, response.RequestCharge);

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveMetadata(string imageId, string tenantId, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(imageId);
            ArgumentException.ThrowIfNullOrEmpty(tenantId);

            var container = await _cosmosDbClient.CreateImagesContainerIfNotExists();
            ItemResponse<ImageMetadata> response;
            try
            {
                response = await container.DeleteItemAsync<ImageMetadata>(imageId, PartitionKeyOf(tenantId, imageId),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return false;

                throw;
            }

            _logger.MetadataRemoved(imageId, response.RequestCharge);

            return response.StatusCode == HttpStatusCode.NoContent;
        }

        private static PartitionKey PartitionKeyOf(string tenantId, string imageId) =>
            new PartitionKeyBuilder().Add(tenantId).Add(imageId).Build();

        private static void Validate(ImageMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentException.ThrowIfNullOrEmpty(metadata.Id);
            ArgumentException.ThrowIfNullOrEmpty(metadata.TenantId);
            ArgumentException.ThrowIfNullOrEmpty(metadata.BlobPath);
            ArgumentException.ThrowIfNullOrEmpty(metadata.MimeType);
            ArgumentException.ThrowIfNullOrEmpty(metadata.MD5Hash);
            if (metadata.SizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.SizeBytes));
            if (metadata.Width <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.Width));
            if (metadata.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(metadata.Height));
        }
    }
}
