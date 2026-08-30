using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using IK.Imager.Api.Extensions;
using IK.Imager.Storage.AzureBlobs;
using IK.Imager.Storage.CosmosDb;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IK.Imager.Api.Tests.Extensions;

/// <summary>
/// There is no switch: each module reads its own endpoint setting, and the presence of that setting is what
/// moves the client off its connection string and onto a DefaultAzureCredential. These tests pin which of
/// the two a module picks, that a module with neither refuses to register, and that the whole process shares
/// one credential.
///
/// Constructing these clients touches no network, and neither does DefaultAzureCredential until a token is
/// asked for, so this runs without Docker and without an Azure account.
/// </summary>
public class ManagedIdentityRegistrationTests
{
    private const string BlobServiceUri = "https://ikimages.blob.core.windows.net";
    private const string CosmosAccountEndpoint = "https://ikimages.documents.azure.com:443/";
    private const string ServiceBusNamespace = "ikimages.servicebus.windows.net";

    private const string CosmosConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2n" +
        "Q9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>
    /// The connection strings alone, which is what appsettings.json ships.
    /// </summary>
    private static IConfiguration Keys(params (string Key, string? Value)[] values)
    {
        var settings = new Dictionary<string, string?>
        {
            ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
            ["AzureStorage:ImagesContainerName"] = "images",
            ["AzureStorage:ThumbnailsContainerName"] = "thumbnails",
            ["CosmosDb:ConnectionString"] = CosmosConnectionString,
            ["CosmosDb:DatabaseId"] = "imagemetadatadb",
            ["CosmosDb:ContainerId"] = "imagemetadatacontainer-v2",
            ["ServiceBus:ConnectionString"] = "Endpoint=sb://ikimages.servicebus.windows.net/;" +
                                              "SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key"
        };

        foreach (var (key, value) in values)
            settings[key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    /// <summary>
    /// The endpoints on top of the connection strings, which is the shape a deployment actually moves
    /// through: one variable added, nothing blanked.
    /// </summary>
    private static IConfiguration Endpoints(params (string Key, string? Value)[] values) =>
        Keys(values.Concat(new (string, string?)[]
        {
            ("AzureStorage:ServiceUri", BlobServiceUri),
            ("CosmosDb:AccountEndpoint", CosmosAccountEndpoint),
            ("ServiceBus:FullyQualifiedNamespace", ServiceBusNamespace)
        }).ToArray());

    private static ServiceProvider BuildStorage(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAzureImageBlobStorage(configuration);
        services.AddCosmosImageMetadataStorage(configuration);

        //what Program.cs asks for
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    [Fact]
    public void AddStorage_WithEndpoints_BuildsTheClientsFromThem()
    {
        using var provider = BuildStorage(Endpoints());

        Assert.Equal(new Uri(BlobServiceUri), provider.GetRequiredService<BlobServiceClient>().Uri);
        Assert.Equal(new Uri(CosmosAccountEndpoint), provider.GetRequiredService<CosmosClient>().Endpoint);
        Assert.IsType<DefaultAzureCredential>(provider.GetRequiredService<TokenCredential>());
    }

    /// <summary>
    /// An endpoint takes precedence, so moving a deployment onto an identity is one variable added rather
    /// than one added and one blanked - and the shipped emulator connection strings stay where they are.
    /// </summary>
    [Fact]
    public void AddStorage_WithBothAnEndpointAndAConnectionString_PrefersTheEndpoint()
    {
        using var provider = BuildStorage(Endpoints());

        Assert.Equal("ikimages", provider.GetRequiredService<BlobServiceClient>().AccountName);
    }

    [Fact]
    public void AddStorage_WithoutEndpoints_BuildsTheClientsFromTheConnectionStrings()
    {
        using var provider = BuildStorage(Keys());

        Assert.Equal("devstoreaccount1", provider.GetRequiredService<BlobServiceClient>().AccountName);
        Assert.Equal(new Uri("https://localhost:8081/"), provider.GetRequiredService<CosmosClient>().Endpoint);
        Assert.Null(provider.GetService<TokenCredential>());
    }

    /// <summary>
    /// One credential for the whole process: it caches the tokens it fetches, and every module TryAdds the
    /// same registration.
    /// </summary>
    [Fact]
    public void AddModules_SeveralOnEndpoints_ShareOneCredential()
    {
        var configuration = Endpoints();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAzureImageBlobStorage(configuration);
        services.AddCosmosImageMetadataStorage(configuration);

        Assert.Single(services, x => x.ServiceType == typeof(TokenCredential));
    }

    /// <summary>
    /// The health probes go through the very clients the repositories use, so they can never reach a
    /// different account - or authenticate differently - than the repositories do.
    /// </summary>
    [Fact]
    public void AddObservability_OnEndpoints_ProbesTheRegisteredClients()
    {
        var configuration = Endpoints();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAzureImageBlobStorage(configuration);
        services.AddCosmosImageMetadataStorage(configuration);
        services.AddObservability(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        Assert.Equal(new Uri(CosmosAccountEndpoint), provider.GetRequiredService<CosmosClient>().Endpoint);
    }

    [Fact]
    public void AddAzureImageBlobStorage_WithNeitherEndpointNorConnectionString_Throws()
    {
        var configuration = Keys(("AzureStorage:ConnectionString", ""));

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddAzureImageBlobStorage(configuration));

        Assert.Contains("AzureStorage:ServiceUri", exception.Message);
        Assert.Contains("AzureStorage:ConnectionString", exception.Message);
    }

    [Fact]
    public void AddAzureImageBlobStorage_WithAServiceUriThatIsNotAUri_Throws()
    {
        var configuration = Keys(("AzureStorage:ServiceUri", "ikimages.blob.core.windows.net"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddAzureImageBlobStorage(configuration));

        Assert.Contains("AzureStorage:ServiceUri", exception.Message);
    }

    [Fact]
    public void AddCosmosImageMetadataStorage_WithNeitherEndpointNorConnectionString_Throws()
    {
        var configuration = Keys(("CosmosDb:ConnectionString", ""));

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddCosmosImageMetadataStorage(configuration));

        Assert.Contains("CosmosDb:AccountEndpoint", exception.Message);
        Assert.Contains("CosmosDb:ConnectionString", exception.Message);
    }

    [Fact]
    public async Task AddIntegrationEventMessaging_WithNeitherNamespaceNorConnectionString_Throws()
    {
        var configuration = Keys(("ServiceBus:ConnectionString", ""));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIntegrationEventMessaging(configuration);

        //MassTransit builds the bus when it is resolved, which is where the host configuration is read.
        //Its own registrations are IAsyncDisposable only, so the provider cannot be disposed synchronously.
        await using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBusControl>());

        Assert.Contains(MessagingServiceCollectionExtensions.ServiceBusNamespacePath, exception.Message);
    }

    /// <summary>
    /// The in-memory transport needs neither, which is what the API integration tests run on.
    /// </summary>
    [Fact]
    public async Task AddIntegrationEventMessaging_InMemoryWithNoServiceBusSettings_StillBuildsTheBus()
    {
        var configuration = Keys(
            ("ServiceBus:ConnectionString", ""),
            ("ServiceBus:Transport", MessagingServiceCollectionExtensions.InMemoryTransport));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIntegrationEventMessaging(configuration);

        await using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IBusControl>());
        Assert.Null(provider.GetService<TokenCredential>());
    }

    /// <summary>
    /// Application Insights needs its connection string either way, so nothing about it can pick the
    /// authentication on its own - hence the one flag that remains.
    /// </summary>
    [Fact]
    public void AddObservability_EntraIdTelemetry_RegistersACredential()
    {
        var configuration = Keys(
            ("Telemetry:ConnectionString", "InstrumentationKey=00000000-0000-0000-0000-000000000000;" +
                                           "IngestionEndpoint=https://localhost/;LiveEndpoint=https://localhost/"),
            ("Telemetry:EnableEntraIdAuthentication", "true"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddObservability(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        Assert.IsType<DefaultAzureCredential>(provider.GetRequiredService<TokenCredential>());
    }
}
