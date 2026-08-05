using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IK.Imager.ImageBlobStorage.AzureFiles;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configuration section this module binds <see cref="ImageAzureStorageSettings"/> from.
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
        services.Configure<ImageAzureStorageSettings>(configuration.GetSection(SectionName));

        //AzureBlobClient takes a raw connection string - the container cannot activate it by type
        services.AddSingleton<IAzureBlobClient>(s =>
            new AzureBlobClient(s.GetRequiredService<IOptions<ImageAzureStorageSettings>>().Value.ConnectionString));

        services.AddScoped<IImageBlobRepository, ImageBlobAzureRepository>();

        return services;
    }
}
