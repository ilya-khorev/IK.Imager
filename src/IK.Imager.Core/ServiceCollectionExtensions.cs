using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Core.ImageSearch;
using IK.Imager.Core.ImageUploading;
using IK.Imager.Core.Messaging;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Thumbnails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IK.Imager.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCoreServices(this IServiceCollection services, IConfiguration namedConfigurationSection)
    {
        services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
        services.AddSingleton<IImageIdentifierProvider, ImageIdentifierProvider>();
        services.AddSingleton<ICdnService, CdnService>();
        services.AddSingleton<IImageResizing, ImageResizing>();

        //todo imageDownloadClient registration?

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        RegisterHandlers(services);

        services.Configure<CdnSettings>(namedConfigurationSection.GetSection("Cdn") ?? namedConfigurationSection);
        services.Configure<ImageThumbnailsSettings>(namedConfigurationSection.GetSection("Thumbnails") ?? namedConfigurationSection);
        services.Configure<ImageLimitationSettings>(namedConfigurationSection.GetSection("ImageLimitations") ?? namedConfigurationSection);

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
