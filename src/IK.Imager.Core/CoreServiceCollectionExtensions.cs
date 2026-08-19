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

namespace IK.Imager.Core;

public static class CoreServiceCollectionExtensions
{
    public const string CdnSectionName = "Cdn";
    public const string ThumbnailsSectionName = "Thumbnails";
    public const string ImageLimitationsSectionName = "ImageLimitations";

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

        services.AddSingleton<IImageInspector, ImageInspector>();
        services.AddSingleton<IImageNameGenerator, ImageNameGenerator>();
        services.AddSingleton<ICdnUrlRewriter, CdnUrlRewriter>();
        services.AddSingleton<IImageResizer, ImageResizer>();

        //ImageValidator takes IOptionsSnapshot, which is scoped - it cannot be a singleton
        services.AddScoped<IImageValidator, ImageValidator>();

        //typed client registered against the interface, so ImageUploader takes an abstraction like all
        //of its other dependencies do
        var imageDownloaderBuilder = services.AddHttpClient<IImageDownloader, ImageDownloader>();
        configureImageDownloader?.Invoke(imageDownloaderBuilder);

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
            new CdnImageLookup(s.GetRequiredService<ImageLookup>(), s.GetRequiredService<ICdnUrlRewriter>()));

        services.AddScoped<ImageUploader>();
        services.AddScoped<IImageUploader>(s =>
            new CdnImageUploader(s.GetRequiredService<ImageUploader>(), s.GetRequiredService<ICdnUrlRewriter>()));
    }
}
