using System;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Deleting;
using IK.Imager.Core.Abstractions.Lookup;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Abstractions.Uploading;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.Deleting;
using IK.Imager.Core.Lookup;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Core.Uploading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IK.Imager.Core;

public static class ServiceCollectionExtensions
{
    public const string CdnSectionName = "Cdn";
    public const string ThumbnailsSectionName = "Thumbnails";
    public const string ImageLimitationsSectionName = "ImageLimitations";

    /// <summary>
    /// Registers the core services - one per feature, plus the pieces they are built from - and binds their settings.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configuration">The configuration root - this module locates its own sections within it.</param>
    /// <param name="configureImageDownloadClient">
    /// Optional hook over the <see cref="ImageDownloadClient"/> typed client. Core owns what the client is;
    /// HTTP resilience is a host policy, so the host passes its retry handling in here.
    /// </param>
    public static IServiceCollection AddImagerCore(this IServiceCollection services, IConfiguration configuration,
        Action<IHttpClientBuilder>? configureImageDownloadClient = null)
    {
        services.Configure<CdnSettings>(configuration.GetSection(CdnSectionName));
        services.Configure<ImageThumbnailsSettings>(configuration.GetSection(ThumbnailsSectionName));
        services.Configure<ImageLimitationSettings>(configuration.GetSection(ImageLimitationsSectionName));

        services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
        services.AddSingleton<IImageIdentifierProvider, ImageIdentifierProvider>();
        services.AddSingleton<ICdnService, CdnService>();
        services.AddSingleton<IImageResizing, ImageResizing>();

        //ImageValidator takes IOptionsSnapshot, which is scoped - it cannot be a singleton
        services.AddScoped<IImageValidator, ImageValidator>();

        var imageDownloadClientBuilder = services.AddHttpClient<ImageDownloadClient>();
        configureImageDownloadClient?.Invoke(imageDownloadClientBuilder);

        RegisterFeatureServices(services);

        return services;
    }

    private static void RegisterFeatureServices(IServiceCollection services)
    {
        services.AddScoped<IImageDeleter, ImageDeleter>();
        services.AddScoped<IThumbnailGenerator, ThumbnailGenerator>();

        //The two services returning image urls are wrapped into a CDN decorator, which is why they are
        //registered by their own type first - see Lookup/CdnImageLookup.cs and Uploading/CdnImageUploader.cs
        services.AddScoped<ImageLookup>();
        services.AddScoped<IImageLookup>(s =>
            new CdnImageLookup(s.GetRequiredService<ImageLookup>(), s.GetRequiredService<ICdnService>()));

        services.AddScoped<ImageUploader>();
        services.AddScoped<IImageUploader>(s =>
            new CdnImageUploader(s.GetRequiredService<ImageUploader>(), s.GetRequiredService<ICdnService>()));
    }
}
