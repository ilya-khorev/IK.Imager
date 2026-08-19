using System.Threading.Tasks;
using Azure.Storage.Blobs;
using IK.Imager.Storage.AzureBlobs;
using IK.Imager.TestsBase;
using Microsoft.Extensions.Options;
using Testcontainers.Azurite;
using Xunit;

namespace IK.Imager.Storage.AzureBlobs.Tests;

/// <summary>
/// Starts a throwaway Azurite container for the whole test assembly and builds the repository
/// under test against it. Nothing is cleaned up explicitly - disposing the container disposes
/// the entire storage account with it.
/// </summary>
public sealed class AzureBlobStorageFixture : IAsyncLifetime
{
    private readonly AzuriteContainer _azurite = new AzuriteBuilder(Constants.AzureBlobStorage.AzuriteImage)
        // Azure.Storage.Blobs sends a newer x-ms-version than Azurite recognises, and Azurite
        // rejects unknown API versions outright. The repository only uses long-stable operations
        // (create container / upload / download / exists / delete), so skipping the check is safe.
        .WithCommand("--skipApiVersionCheck")
        // The emulator is discarded at the end of the run, so there is no point writing to disk.
        .WithInMemoryPersistence()
        .Build();

    /// <summary>
    /// The system under test.
    /// </summary>
    public AzureBlobImageRepository Repository { get; private set; } = null!;

    /// <summary>
    /// A raw client, for the assertions that need to look at the storage account
    /// without going through the repository.
    /// </summary>
    public BlobServiceClient BlobServiceClient { get; private set; } = null!;

    public AzureBlobStorageSettings Settings { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();

        //Points at the randomly mapped host port, e.g. BlobEndpoint=http://localhost:32773/devstoreaccount1
        var connectionString = _azurite.GetConnectionString();

        Settings = new AzureBlobStorageSettings
        {
            ConnectionString = connectionString,
            ImagesContainerName = Constants.AzureBlobStorage.ImagesContainerName,
            ThumbnailsContainerName = Constants.AzureBlobStorage.ThumbnailsContainerName
        };

        BlobServiceClient = new BlobServiceClient(connectionString);
        Repository = new AzureBlobImageRepository(
            new OptionsWrapper<AzureBlobStorageSettings>(Settings),
            new BlobContainerFactory(connectionString));
    }

    public Task DisposeAsync() => _azurite.DisposeAsync().AsTask();
}
