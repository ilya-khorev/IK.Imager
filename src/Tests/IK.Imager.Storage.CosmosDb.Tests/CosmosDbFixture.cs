using System;
using System.Threading.Tasks;
using IK.Imager.TestsBase;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Testcontainers.CosmosDb;
using Xunit;

namespace IK.Imager.Storage.CosmosDb.Tests;

/// <summary>
/// Starts a throwaway Linux ("vNext") Cosmos DB emulator for the whole test assembly and builds the
/// repository under test against it. Neither the database nor the container is dropped explicitly -
/// disposing the emulator throws away the whole account.
/// </summary>
public sealed class CosmosDbFixture : IAsyncLifetime
{
    private readonly CosmosDbContainer _emulator = new CosmosDbBuilder(Constants.CosmosDb.EmulatorImage).Build();

    private CosmosClient _client = null!;

    /// <summary>
    /// The system under test.
    /// </summary>
    public CosmosImageMetadataRepository Repository { get; private set; } = null!;

    /// <summary>
    /// A raw client, for the assertions that need to look at the account without going through the repository.
    /// </summary>
    public CosmosClient CosmosClient => _client;

    public CosmosDbSettings Settings { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _emulator.StartAsync();

        Settings = new CosmosDbSettings
        {
            //Points at the randomly mapped host port, e.g. AccountEndpoint=http://localhost:32774/
            ConnectionString = _emulator.GetConnectionString(),
            ContainerId = Constants.CosmosDb.ContainerId,
            ContainerThroughputOnCreation = Constants.CosmosDb.ContainerThroughputOnCreation,
            DatabaseId = Constants.CosmosDb.DatabaseId
        };

        Repository = new CosmosImageMetadataRepository(
            new ImageContainerFactory(new OptionsWrapper<CosmosDbSettings>(Settings), CreateEmulatorClientOptions()));

        _client = new CosmosClient(Settings.ConnectionString, CreateEmulatorClientOptions());
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _emulator.DisposeAsync();
    }

    /// <summary>
    /// The options the .NET SDK needs to talk to the containerized emulator.
    ///
    /// The emulator only supports gateway mode, and it advertises its container-internal endpoint
    /// (http://127.0.0.1:8081/) in the database account document, which is not reachable from the host.
    /// LimitToEndpoint keeps the client on the endpoint it was given, and the http message handler
    /// provided by the Testcontainers module rewrites anything that still slips through to the mapped port.
    ///
    /// The serializer is deliberately left alone - ImageMetadata.Id is mapped through Newtonsoft's
    /// [JsonProperty("id")], so the SDK default (Newtonsoft based) serializer is load bearing.
    /// </summary>
    private CosmosClientOptions CreateEmulatorClientOptions() =>
        new()
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true,
            HttpClientFactory = () => _emulator.HttpClient,
            RequestTimeout = TimeSpan.FromMinutes(2)
        };
}
