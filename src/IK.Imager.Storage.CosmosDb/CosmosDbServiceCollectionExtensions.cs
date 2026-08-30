using System;
using Azure.Core;
using Azure.Identity;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IK.Imager.Storage.CosmosDb;

public static class CosmosDbServiceCollectionExtensions
{
    /// <summary>
    /// Configuration section this module binds <see cref="CosmosDbSettings"/> from.
    /// </summary>
    public const string SectionName = "CosmosDb";

    /// <summary>
    /// Registers the Cosmos DB implementation of <see cref="IImageMetadataRepository"/>
    /// and binds its settings from the <see cref="SectionName"/> section.
    /// </summary>
    /// <remarks>
    /// <see cref="CosmosDbSettings.AccountEndpoint"/> is what picks the authentication: set it and the
    /// account is reached with <see cref="DefaultAzureCredential"/>, leave it empty and the connection
    /// string is used. The endpoint wins when both are set.
    /// </remarks>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddCosmosImageMetadataStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        services.Configure<CosmosDbSettings>(section);

        var accountEndpoint = ReadAccountEndpoint(section);
        if (accountEndpoint != null)
            //one credential for every Azure client in the process - it caches the tokens it fetches.
            //TryAdd, so a host that wants a credential of its own can register it before this runs.
            services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

        //the client is registered rather than built inside ImageContainerFactory so that the health check
        //probes the same account, with the same credential, as the repository. The only SDK option set is
        //the serializer - the integration tests replace this registration to reach the emulator.
        services.AddSingleton(s => accountEndpoint == null
            ? new CosmosClient(s.GetRequiredService<IOptions<CosmosDbSettings>>().Value.ConnectionString,
                ImageMetadataSerialization.CreateClientOptions())
            //data plane access only - creating the database and the container needs the control plane,
            //so an account reached with an identity has to be provisioned up front
            : new CosmosClient(accountEndpoint, s.GetRequiredService<TokenCredential>(),
                ImageMetadataSerialization.CreateClientOptions()));

        services.AddSingleton<IImageContainerFactory>(s => new ImageContainerFactory(
            s.GetRequiredService<CosmosClient>(),
            s.GetRequiredService<IOptions<CosmosDbSettings>>(),
            provision: accountEndpoint == null));

        services.AddScoped<IImageMetadataRepository, CosmosImageMetadataRepository>();

        return services;
    }

    private static string? ReadAccountEndpoint(IConfigurationSection section)
    {
        var accountEndpoint = section["AccountEndpoint"];

        if (string.IsNullOrWhiteSpace(accountEndpoint))
        {
            if (string.IsNullOrWhiteSpace(section["ConnectionString"]))
                throw new InvalidOperationException(
                    $"Cosmos DB is not configured. Set '{SectionName}:AccountEndpoint' to reach the account " +
                    $"with a managed identity, or '{SectionName}:ConnectionString' to reach it with a key.");

            return null;
        }

        if (!Uri.IsWellFormedUriString(accountEndpoint, UriKind.Absolute))
            throw new InvalidOperationException(
                $"'{SectionName}:AccountEndpoint' is not an absolute uri. It is the endpoint of the " +
                "account, e.g. https://myaccount.documents.azure.com:443/.");

        return accountEndpoint;
    }
}
