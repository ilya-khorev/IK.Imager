using System.Threading.Tasks;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.IntegrationEvents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace IK.Imager.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await RegisterHandlers(host);
            await host.RunAsync();
        }

        private static async Task RegisterHandlers(IHost host)
        {
            var eventBus = host.Services.GetRequiredService<IEventBus>();
            var topicsConfiguration = host.Services.GetRequiredService<IOptions<TopicsConfiguration>>();
            int maxConcurrentCalls = topicsConfiguration.Value.MaxConcurrentCalls;

            IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent> imageUploadedEventHandler = host.Services.GetRequiredService<IIntegrationEventHandler<OriginalImageUploadedIntegrationEvent>>();
            IIntegrationEventHandler<ImageDeletedIntegrationEvent> imageDeletedEventHandler = host.Services.GetRequiredService<IIntegrationEventHandler<ImageDeletedIntegrationEvent>>();
            
            await eventBus.Subscribe(topicsConfiguration.Value.UploadedImagesTopicName, topicsConfiguration.Value.SubscriptionName, 
                imageUploadedEventHandler, maxConcurrentCalls);
            await eventBus.Subscribe(topicsConfiguration.Value.DeletedImagesTopicName, topicsConfiguration.Value.SubscriptionName, 
                imageDeletedEventHandler, maxConcurrentCalls);

            var logger = host.Services.GetService<ILogger<Program>>();
            logger.LogInformation("Integration event handlers were started successfully");
        }
        
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((c, l) =>
                {
                    l.ClearProviders();
                    l.AddConfiguration(c.Configuration);
                    l.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}