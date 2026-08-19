using IK.Imager.Storage.Abstractions.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - the module locates its own section within it.</param>
    public static IServiceCollection AddCosmosImageMetadataStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CosmosDbSettings>(configuration.GetSection(SectionName));

        //ImageContainerFactory takes an optional CosmosClientOptions, which the container cannot bind -
        //production leaves it null and gets the SDK defaults
        services.AddSingleton<IImageContainerFactory>(s =>
            new ImageContainerFactory(s.GetRequiredService<IOptions<CosmosDbSettings>>()));

        services.AddScoped<IImageMetadataRepository, CosmosImageMetadataRepository>();

        return services;
    }
}
