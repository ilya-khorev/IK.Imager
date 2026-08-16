using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddAzureImageBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureBlobStorageSettings>(configuration.GetSection(SectionName));

        //BlobContainerFactory takes a raw connection string - the container cannot activate it by type
        services.AddSingleton<IBlobContainerFactory>(s =>
            new BlobContainerFactory(s.GetRequiredService<IOptions<AzureBlobStorageSettings>>().Value.ConnectionString));

        services.AddScoped<IImageBlobRepository, AzureBlobImageRepository>();

        return services;
    }
}
