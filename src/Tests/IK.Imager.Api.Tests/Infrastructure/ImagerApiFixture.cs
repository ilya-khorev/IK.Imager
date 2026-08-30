using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IK.Imager.Api.Extensions;
using IK.Imager.TestsBase;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.Azurite;
using Testcontainers.CosmosDb;
using Xunit;

namespace IK.Imager.Api.Tests.Infrastructure;

/// <summary>
/// Boots the whole service once for the test assembly: Azurite and the Cosmos DB emulator as throwaway
/// containers, and IK.Imager.Api itself in-process on top of them through <see cref="WebApplicationFactory{T}"/>.
/// The tests then talk to it over HTTP exactly as a client would.
///
/// The one dependency that is not a container is Azure Service Bus. MassTransit 8 cannot drive the Service
/// Bus emulator - the emulator only grew an administration API in 2026 and MassTransit only speaks to it
/// from v9 - so the host is started on the in-memory transport instead, via the ServiceBus:Transport switch
/// the API already exposes. The consumers, the integration events and the asynchronous publish/consume path
/// are the production ones either way; only the wire underneath them differs.
/// </summary>
public sealed class ImagerApiFixture : IAsyncLifetime
{
    private const string ImagesContainerName = "apitestimages";
    private const string ThumbnailsContainerName = "apitestthumbnails";
    private const string DatabaseId = "ApiTestImageMetadataDb";
    private const string ContainerId = "ApiTestImageMetadataContainer";

    private readonly AzuriteContainer _azurite = new AzuriteBuilder(Constants.AzureBlobStorage.AzuriteImage)
        // Azure.Storage.Blobs sends a newer x-ms-version than Azurite recognises - same reason as in
        // AzureBlobStorageFixture.
        .WithCommand("--skipApiVersionCheck")
        .WithInMemoryPersistence()
        .Build();

    private readonly CosmosDbContainer _cosmos = new CosmosDbBuilder(Constants.CosmosDb.EmulatorImage).Build();

    private readonly Dictionary<string, string?> _replacedVariables = new();

    private WebApplicationFactory<Program> _factory = null!;

    /// <summary>
    /// A client bound to the running API. The tests share it - nothing they do leaves per-client state behind.
    /// </summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Completes when a published integration event has actually been consumed, which is what makes the
    /// asynchronous half of the system assertable without sleeping. See <see cref="ConsumedEventObserver"/>.
    /// </summary>
    public ConsumedEventObserver ConsumedEvents { get; } = new();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_azurite.StartAsync(), _cosmos.StartAsync());

        ApplyConfiguration();

        _factory = new ImagerApiApplicationFactory(CreateEmulatorClient());
        Client = _factory.CreateClient();

        //bus level observers reach every receive endpoint; connected after CreateClient so that the host,
        //and with it the bus, is already running
        _factory.Services.GetRequiredService<IBus>().ConnectConsumeObserver(ConsumedEvents);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory != null)
            await _factory.DisposeAsync();

        RestoreConfiguration();

        await Task.WhenAll(_azurite.DisposeAsync().AsTask(), _cosmos.DisposeAsync().AsTask());
    }

    /// <summary>
    /// Points the service at the two containers and off Azure Service Bus.
    ///
    /// Environment variables rather than WebApplicationFactory's UseSetting: with minimal hosting, Program.cs
    /// has already read builder.Configuration and registered every module by the time the factory gets a
    /// chance to contribute settings, whereas WebApplication.CreateBuilder reads the environment on the way
    /// in. This is also the documented way to configure a deployed instance, '__' separator and all.
    /// </summary>
    private void ApplyConfiguration()
    {
        //Points at the randomly mapped host ports, e.g. BlobEndpoint=http://127.0.0.1:32773/devstoreaccount1
        Set("AzureStorage__ConnectionString", _azurite.GetConnectionString());
        Set("AzureStorage__ImagesContainerName", ImagesContainerName);
        Set("AzureStorage__ThumbnailsContainerName", ThumbnailsContainerName);

        Set("CosmosDb__ConnectionString", _cosmos.GetConnectionString());
        Set("CosmosDb__DatabaseId", DatabaseId);
        Set("CosmosDb__ContainerId", ContainerId);

        Set("ServiceBus__Transport", MessagingServiceCollectionExtensions.InMemoryTransport);

        //Azurite serves the image the upload-by-url tests download on 127.0.0.1, and the download refuses
        //private addresses everywhere else - see ImageDownloadSettings.AllowPrivateAddresses
        Set("ImageDownload__AllowPrivateAddresses", "true");

        //both, because AddImagerTelemetry falls back to the variable the Azure Monitor distro reads on its
        //own - otherwise a workstation that has it set exports every test run into a real workspace
        Set("Telemetry__ConnectionString", string.Empty);
        Set("APPLICATIONINSIGHTS_CONNECTION_STRING", string.Empty);
    }

    private void Set(string name, string value)
    {
        _replacedVariables[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    private void RestoreConfiguration()
    {
        foreach (var (name, value) in _replacedVariables)
            Environment.SetEnvironmentVariable(name, value);

        _replacedVariables.Clear();
    }

    /// <summary>
    /// The client the .NET SDK needs to talk to the containerized Cosmos emulator - gateway mode, pinned to
    /// the endpoint it was given, and the module's URI-rewriting handler on top. Identical to what
    /// CosmosDbFixture builds. Production takes the client the Cosmos module registers, with the SDK defaults.
    /// </summary>
    private CosmosClient CreateEmulatorClient() =>
        new(_cosmos.GetConnectionString(), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true,
            HttpClientFactory = () => _cosmos.HttpClient,
            RequestTimeout = TimeSpan.FromMinutes(2)
        });

    /// <summary>
    /// Runs the real Program.cs and swaps in the only registration that cannot be expressed as configuration:
    /// the Cosmos client, because its emulator options are SDK objects rather than settings. Everything
    /// built on top of it - the container factory, the repository and the health check - is the production one.
    /// </summary>
    private sealed class ImagerApiApplicationFactory(CosmosClient cosmosClient) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<CosmosClient>();
                services.AddSingleton(cosmosClient);
            });
        }
    }
}
