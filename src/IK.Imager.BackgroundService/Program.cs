using System.Threading.Tasks;
using IK.Imager.BackgroundService.Configuration;
using IK.Imager.BackgroundService.Handlers;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.IntegrationEvents;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.EventBus.AzureServiceBus;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using IK.Imager.Storage.Abstractions.Storage;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IK.Imager.BackgroundService
{
   public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args)
                .Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.Sources.Clear();
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((c, l) =>
                {
                    l.ClearProviders();
                    l.AddConfiguration(c.Configuration);
                    l.AddConsole(options => options.TimestampFormat = "[dd-MM-yyyy HH:mm:ss.fff] ");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    RegisterConfigurations(hostContext.Configuration, services);
                    SetupAppInsights(hostContext.Configuration, services);
                    services.AddSingleton<IEventBus, ServiceBus>();
                    services.AddSingleton<IImageMetadataStorage, ImageMetadataCosmosDbStorage>();
                    services.AddSingleton<IImageBlobStorage, ImageBlobAzureStorage>();
                    services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
                    services.AddSingleton<IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent>, GenerateThumbnailsHandler>();
                    services.AddSingleton<IIntegrationEventHandler<ImageDeletedIntegrationEvent>, RemoveImageFilesHandler>();
                })
                .UseConsoleLifetime();
        
        private static void SetupAppInsights(IConfiguration configuration, IServiceCollection services)
        {
            ApplicationInsightsServiceOptions aiOptions = new ApplicationInsightsServiceOptions();
            var appInsightsDependencyConfigValue = configuration.GetValue<bool>("ApplicationInsights:EnableDependencyTrackingTelemetryModule");
            //dependency tracking is disabled by default as it is quite expensive
            aiOptions.EnableDependencyTrackingTelemetryModule = appInsightsDependencyConfigValue;

            //by default instrumentation key is taken from config
            //Alternatively, specify the instrumentation key in either of the following environment variables.
            //APPINSIGHTS_INSTRUMENTATIONKEY or ApplicationInsights:InstrumentationKey
            services.AddApplicationInsightsTelemetryWorkerService(aiOptions);

            var appInsightsAuthApiKey = configuration.GetValue<string>("ApplicationInsights:AuthenticationApiKey");
            if (!string.IsNullOrWhiteSpace(appInsightsAuthApiKey))
                services.ConfigureTelemetryModule<QuickPulseTelemetryModule> ((module, o) => 
                    module.AuthenticationApiKey = appInsightsAuthApiKey);
        }
        
        private static void RegisterConfigurations(IConfiguration configuration, IServiceCollection services)
        {
            services.Configure<ServiceBusSettings>(configuration.GetSection("ServiceBus"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ServiceBusSettings>>().Value);
            
            services.Configure<ImageAzureStorageConfiguration>(configuration.GetSection("AzureStorage"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ImageAzureStorageConfiguration>>().Value);
            
            services.Configure<ImageMetadataCosmosDbStorageConfiguration>(configuration.GetSection("CosmosDb"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ImageMetadataCosmosDbStorageConfiguration>>().Value);
            
            services.Configure<TopicsConfiguration>(configuration.GetSection("Topics"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<TopicsConfiguration>>().Value);
            
            services.Configure<ImageThumbnailsSettings>(configuration.GetSection("Thumbnails"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ImageThumbnailsSettings>>().Value);
        }
    }
}