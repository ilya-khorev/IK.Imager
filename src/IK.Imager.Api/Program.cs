using System;
using System.IO;
using System.Reflection;
using Azure.Storage.Blobs;
using FluentValidation;
using HealthChecks.Azure.Storage.Blobs;
using HealthChecks.CosmosDb;
using HealthChecks.UI.Client;
using IK.Imager.Api;
using IK.Imager.Api.DomainEventHandlers;
using IK.Imager.Api.Filters;
using IK.Imager.Api.IntegrationEvents;
using IK.Imager.Api.IntegrationEvents.EventHandling;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Api.Middleware;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Core.ImageUploading;
using IK.Imager.Core.Validation;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using IK.Imager.Storage.Abstractions.Repositories;
using MassTransit;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Polly;

#pragma warning disable 1591

const string apiTitle = "IK.Imager API";
const string currentVersion = "v1.0";

var builder = WebApplication.CreateBuilder(args);

//the default builder registers appsettings.json as optional - this service cannot start without it
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddSystemdConsole(options => options.TimestampFormat = "[dd-MM-yyyy HH:mm:ss.fff] ");

var configuration = builder.Configuration;
var services = builder.Services;
var apiAssembly = Assembly.GetExecutingAssembly();

services.AddControllers(options =>
{
    options.Filters.Add(typeof(GlobalExceptionFilter));
    options.Filters.Add(typeof(FluentValidationActionFilter));
});

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(currentVersion, new OpenApiInfo {Title = apiTitle, Version = currentVersion});
    foreach (var contractFile in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "IK.Imager.*.xml", SearchOption.AllDirectories))
        options.IncludeXmlComments(contractFile);
});

services.Configure<ImageAzureStorageSettings>(configuration.GetSection("AzureStorage"));
services.Configure<ImageMetadataCosmosDbStorageSettings>(configuration.GetSection("CosmosDb"));
services.Configure<TopicsConfiguration>(configuration.GetSection("Topics"));

//CosmosDbClient takes an optional CosmosClientOptions, which the DI container cannot bind - register it explicitly
services.AddSingleton<ICosmosDbClient>(s =>
    new CosmosDbClient(s.GetRequiredService<IOptions<ImageMetadataCosmosDbStorageSettings>>()));
services.AddSingleton<IAzureBlobClient, AzureBlobClient>(s =>
{
    var settings = s.GetRequiredService<IOptions<ImageAzureStorageSettings>>();
    return new AzureBlobClient(settings.Value.ConnectionString);
});

services.RegisterCoreServices(configuration);

services.AddScoped<IImageBlobRepository, ImageBlobAzureRepository>();
services.AddScoped<IImageMetadataRepository, ImageMetadataCosmosDbRepository>();
services.AddScoped<IImageValidator, ImageValidator>();

//Domain events raised by the core handlers are translated into Service Bus integration events here
services.AddScoped<IDomainEventHandler<ImageUploadedDomainEvent>, ImageUploadedDomainEventHandler>();
services.AddScoped<IDomainEventHandler<ImageMetadataDeletedDomainEvent>, ImageMetadataDeletedDomainEventHandler>();

services.AddHttpClient<ImageDownloadClient>()
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)));

services.AddValidatorsFromAssembly(apiAssembly);
services.AddFluentValidationRulesToSwagger();

services.AddHealthChecks(configuration);
services.SetupAppInsights(configuration);

services.AddMassTransit(x =>
{
    x.AddConsumers(apiAssembly);
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(configuration.GetValue<string>("ServiceBus:ConnectionString"));

        var topicsConfiguration = context.GetRequiredService<IOptions<TopicsConfiguration>>();

        cfg.Message<OriginalImageUploadedIntegrationEvent>(c =>
            c.SetEntityName(topicsConfiguration.Value.UploadedImagesTopicName));
        cfg.Message<ImageMetadataDeletedIntegrationEvent>(c =>
            c.SetEntityName(topicsConfiguration.Value.DeletedImagesTopicName));

        cfg.ConcurrentMessageLimit = topicsConfiguration.Value.MaxConcurrentCalls;

        cfg.SubscriptionEndpoint<OriginalImageUploadedIntegrationEvent>(topicsConfiguration.Value.SubscriptionName,
            configurator =>
            {
                configurator.ConfigureConsumer<CreateThumbnailsHandler>(context);
            });
        cfg.SubscriptionEndpoint<ImageMetadataDeletedIntegrationEvent>(topicsConfiguration.Value.SubscriptionName,
            configurator =>
            {
                configurator.ConfigureConsumer<RemoveImageFilesHandler>(context);
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"/swagger/{currentVersion}/swagger.json", apiTitle);
    c.RoutePrefix = string.Empty;
});

app.UseMiddleware<ServiceFabricResourceNotFoundMiddleware>();

app.MapControllers();
app.MapHealthChecks("/hc", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("self")
});

await app.RunAsync();

namespace IK.Imager.Api
{
    public static class CustomExtensionsMethods
    {
        public static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());

            var cosmosDbConnectionString = configuration["CosmosDb:ConnectionString"];
            var cosmosDbDatabase = configuration["CosmosDb:DatabaseId"];
            hcBuilder.AddAzureCosmosDB(
                _ => new CosmosClient(cosmosDbConnectionString),
                _ => new AzureCosmosDbHealthCheckOptions { DatabaseId = cosmosDbDatabase },
                "ik.imager-cosmossdb-check", tags: new[] { "cosmosdb" });

            var azureConnectionString = configuration["AzureStorage:ConnectionString"];
            var azureContainerName = configuration["AzureStorage:ImagesContainerName"];
            hcBuilder.AddAzureBlobStorage(
                _ => new BlobServiceClient(azureConnectionString),
                _ => new AzureBlobStorageHealthCheckOptions { ContainerName = azureContainerName },
                "ik.imager-blobstorage-check", tags: new[] { "blobstorage" });
        }

        public static void SetupAppInsights(this IServiceCollection services, IConfiguration configuration)
        {
            ApplicationInsightsServiceOptions aiOptions = new ApplicationInsightsServiceOptions();
            var appInsightsDependencyConfigValue = configuration.GetValue<bool>("ApplicationInsights:EnableDependencyTrackingTelemetryModule");
            //dependency tracking is disabled by default as it is produce a lot of logs and therefore quite expensive
            aiOptions.EnableDependencyTrackingTelemetryModule = appInsightsDependencyConfigValue;

            //By default, instrumentation key is taken from the configuration
            //Alternatively, specify the instrumentation key in either of the following environment variables:
            //APPINSIGHTS_INSTRUMENTATIONKEY or ApplicationInsights:InstrumentationKey
            services.AddApplicationInsightsTelemetry(aiOptions);

            var appInsightsAuthApiKey = configuration.GetValue<string>("ApplicationInsights:AuthenticationApiKey");
            if (!string.IsNullOrWhiteSpace(appInsightsAuthApiKey))
                services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, _) =>
                    module.AuthenticationApiKey = appInsightsAuthApiKey);
        }
    }
}
