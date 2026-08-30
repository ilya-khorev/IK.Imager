using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IK.Imager.Storage.AzureBlobs;

public static class AzureBlobStorageServiceCollectionExtensions
{
    /// <summary>
    /// Configuration section this module binds <see cref="AzureBlobStorageSettings"/> from.
    /// </summary>
    public const string SectionName = "AzureStorage";

    /// <summary>
    /// Registers the Azure Blob Storage implementation of <see cref="IImageBlobRepository"/>
    /// and binds its settings from the <see cref="SectionName"/> section.
    /// </summary>
    /// <remarks>
    /// <see cref="AzureBlobStorageSettings.ServiceUri"/> is what picks the authentication: set it and the
    /// account is reached with <see cref="DefaultAzureCredential"/>, leave it empty and the connection
    /// string is used. The endpoint wins when both are set, so a deployment can move to an identity by
    /// adding one variable rather than by also blanking the connection string.
    /// </remarks>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddAzureImageBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        services.Configure<AzureBlobStorageSettings>(section);

        var serviceUri = ReadServiceUri(section);
        if (serviceUri != null)
            //one credential for every Azure client in the process - it caches the tokens it fetches.
            //TryAdd, so a host that wants a credential of its own can register it before this runs.
            services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

        //the client is registered rather than built inside BlobContainerFactory so that the health check
        //probes the same account, with the same credential, as the repository
        services.AddSingleton(s => serviceUri == null
            ? new BlobServiceClient(s.GetRequiredService<IOptions<AzureBlobStorageSettings>>().Value.ConnectionString)
            : new BlobServiceClient(serviceUri, s.GetRequiredService<TokenCredential>()));

        services.AddSingleton<IBlobContainerFactory, BlobContainerFactory>();

        services.AddScoped<IImageBlobRepository, AzureBlobImageRepository>();

        return services;
    }

    private static Uri? ReadServiceUri(IConfigurationSection section)
    {
        var serviceUri = section["ServiceUri"];

        if (string.IsNullOrWhiteSpace(serviceUri))
        {
            if (string.IsNullOrWhiteSpace(section["ConnectionString"]))
                throw new InvalidOperationException(
                    $"Blob storage is not configured. Set '{SectionName}:ServiceUri' to reach the account " +
                    $"with a managed identity, or '{SectionName}:ConnectionString' to reach it with a key.");

            return null;
        }

        if (!Uri.TryCreate(serviceUri, UriKind.Absolute, out var parsed))
            throw new InvalidOperationException(
                $"'{SectionName}:ServiceUri' is not an absolute uri. It is the blob endpoint of the " +
                "account, e.g. https://myaccount.blob.core.windows.net.");

        return parsed;
    }
}
