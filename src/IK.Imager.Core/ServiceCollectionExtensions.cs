using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.ImageUploading;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Thumbnails;
using MediatR;
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
    
        services.AddMediatR(typeof(UploadImageCommand).Assembly);  
        
        services.Configure<CdnSettings>(namedConfigurationSection.GetSection("Cdn") ?? namedConfigurationSection);
        services.Configure<ImageThumbnailsSettings>(namedConfigurationSection.GetSection("Thumbnails") ?? namedConfigurationSection);
        services.Configure<ImageLimitationSettings>(namedConfigurationSection.GetSection("ImageLimitations") ?? namedConfigurationSection);

        return services;
    }
}