using System;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Delete;
using IK.Imager.Core.Abstractions.Lookup;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Abstractions.Upload;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.Delete;
using IK.Imager.Core.Lookup;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Core.Upload;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core;

public static class CoreServiceCollectionExtensions
{
    public const string CdnSectionName = "Cdn";
    public const string ThumbnailsSectionName = "Thumbnails";
    public const string ImageLimitationsSectionName = "ImageLimitations";
    public const string ImageDownloadSectionName = "ImageDownload";

    /// <summary>
    /// Registers the core services - one per feature, plus the pieces they are built from - and binds their settings.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - this module locates its own sections within it.</param>
    /// <param name="configureImageDownloader">
    /// Optional hook over the <see cref="ImageDownloader"/> typed client. Core owns what the client is;
    /// HTTP resilience is a host policy, so the host passes its retry handling in here.
    /// </param>
    public static IServiceCollection AddImagerCore(this IServiceCollection services, IConfiguration configuration,
        Action<IHttpClientBuilder>? configureImageDownloader = null)
    {
        services.Configure<CdnSettings>(configuration.GetSection(CdnSectionName));
        services.Configure<ImageThumbnailsSettings>(configuration.GetSection(ThumbnailsSectionName));
        services.Configure<ImageLimitationsSettings>(configuration.GetSection(ImageLimitationsSectionName));
        services.Configure<ImageDownloadSettings>(configuration.GetSection(ImageDownloadSectionName));

        services.AddSingleton<IImageIdGenerator, ImageIdGenerator>();
        services.AddSingleton<IImageResizer, ImageResizer>();

        //TryAdd so that a provider module can register its own purger without Core knowing it exists
        services.TryAddSingleton<ICdnPurger, NoOpCdnPurger>();

        //ImageValidator takes IOptionsSnapshot, which is scoped - neither it nor the inspector built
        //on it can be a singleton
        services.AddScoped<ImageValidator>();
        services.AddScoped<IImageInspector, ImageInspector>();

        //scoped because IImageBlobRepository is
        services.AddScoped<IImageUrlBuilder, ImageUrlBuilder>();

        RegisterImageDownloader(services, configureImageDownloader);
        RegisterFeatureServices(services);

        return services;
    }

    /// <summary>
    /// A typed client registered against the interface, so <see cref="ImageUploader"/> takes an abstraction
    /// like all of its other dependencies do.
    ///
    /// The timeout and the primary handler are what the client is rather than host policy: the url comes from
    /// the caller, so a client with no time bound and no address checks is a way into the deployment. The host
    /// hook runs last and can still replace either.
    /// </summary>
    private static void RegisterImageDownloader(IServiceCollection services,
        Action<IHttpClientBuilder>? configureImageDownloader)
    {
        var builder = services.AddHttpClient<IImageDownloader, ImageDownloader>()
            .ConfigureHttpClient((provider, client) => client.Timeout = DownloadSettings(provider).Timeout)
            .ConfigurePrimaryHttpMessageHandler(provider =>
                ImageDownloadHandler.Create(DownloadSettings(provider).AllowPrivateAddresses));

        configureImageDownloader?.Invoke(builder);
    }

    private static ImageDownloadSettings DownloadSettings(IServiceProvider provider) =>
        provider.GetRequiredService<IOptionsMonitor<ImageDownloadSettings>>().CurrentValue;

    private static void RegisterFeatureServices(IServiceCollection services)
    {
        services.AddScoped<IImageUploader, ImageUploader>();
        services.AddScoped<IImageLookup, ImageLookup>();
        services.AddScoped<IImageDeleter, ImageDeleter>();
        services.AddScoped<IThumbnailGenerator, ThumbnailGenerator>();
    }
}
