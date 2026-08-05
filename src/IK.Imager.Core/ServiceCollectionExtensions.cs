using System;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Core.ImageSearch;
using IK.Imager.Core.ImageUploading;
using IK.Imager.Core.Messaging;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IK.Imager.Core;

public static class ServiceCollectionExtensions
{
    public const string CdnSectionName = "Cdn";
    public const string ThumbnailsSectionName = "Thumbnails";
    public const string ImageLimitationsSectionName = "ImageLimitations";

    /// <summary>
    /// Registers the core services - handlers, thumbnail resizing, validation, CDN - and binds their settings.
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
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        var imageDownloadClientBuilder = services.AddHttpClient<ImageDownloadClient>();
        configureImageDownloadClient?.Invoke(imageDownloadClientBuilder);

        RegisterHandlers(services);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<DeleteImageCommand>, DeleteImageCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteImageMetadataCommand, bool>, DeleteImageMetadataCommandHandler>();
        services.AddScoped<ICommandHandler<CreateThumbnailsCommand>, CreateThumbnailsCommandHandler>();

        //Handlers returning image urls are wrapped into a CDN decorator - see IK.Imager.Core/Cdn/CdnDecorators.cs
        services.AddScoped<RequestImagesQueryHandler>();
        services.AddScoped<IQueryHandler<RequestImagesQuery, ImagesSearchResult>>(s =>
            new RequestImagesQueryCdnDecorator(s.GetRequiredService<RequestImagesQueryHandler>(), s.GetRequiredService<ICdnService>()));

        services.AddScoped<UploadImageCommandHandler>();
        services.AddScoped<ICommandHandler<UploadImageCommand, ImageInfo>>(s =>
            new UploadImageCommandCdnDecorator(s.GetRequiredService<UploadImageCommandHandler>(), s.GetRequiredService<ICdnService>()));

        services.AddScoped<UploadImageByUrlCommandHandler>();
        services.AddScoped<ICommandHandler<UploadImageByUrlCommand, ImageInfo>>(s =>
            new UploadImageByUrlCommandCdnDecorator(s.GetRequiredService<UploadImageByUrlCommandHandler>(), s.GetRequiredService<ICdnService>()));
    }
}
