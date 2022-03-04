using System;
using System.IO;
using System.Reflection;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using IK.Imager.Api.Filters;
using IK.Imager.Api.IntegrationEvents;
using IK.Imager.Api.IntegrationEvents.EventHandling;
using IK.Imager.Api.IntegrationEvents.Events;
using IK.Imager.Api.Middleware;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.ImageDeleting;
using IK.Imager.Core.Abstractions.ImageSearch;
using IK.Imager.Core.Abstractions.ImageUploading;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.ImageDeleting;
using IK.Imager.Core.ImageSearch;
using IK.Imager.Core.ImageUploading;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Core.Validation;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.Storage.Abstractions.Repositories;
using MassTransit;
using MediatR;
using MicroElements.Swashbuckle.FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;
using OriginalImageUploadedIntegrationEvent = IK.Imager.Api.IntegrationEvents.Events.OriginalImageUploadedIntegrationEvent;

#pragma warning disable 1591

namespace IK.Imager.Api
{
    public class Startup
    {
        private const string ApiTitle = "IK.Imager API";
        private const string CurrentVersion = "v1.0";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options => { options.Filters.Add(typeof(GlobalExceptionFilter)); });
            
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(CurrentVersion, new OpenApiInfo {Title = ApiTitle, Version = CurrentVersion});
                foreach (var contractFile in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "IK.Imager.*.xml", SearchOption.AllDirectories))
                    options.IncludeXmlComments(contractFile);
            });

            services.AddAutoMapper(c => c.AddProfile<MappingProfile>(), typeof(Startup));

            RegisterConfigurations(services);
            
            services.AddSingleton<ICosmosDbClient, CosmosDbClient>();
            services.AddSingleton<IAzureBlobClient, AzureBlobClient>(s =>
            {
                var settings = s.GetRequiredService<IOptions<ImageAzureStorageSettings>>();
                return new AzureBlobClient(settings.Value.ConnectionString);
            });
                
            services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
            services.AddSingleton<IImageIdentifierProvider, ImageIdentifierProvider>();
            services.AddSingleton<ICdnService, CdnService>();
            services.AddSingleton<IImageResizing, ImageResizing>();
            
            services.AddScoped<IImageBlobRepository, ImageBlobAzureRepository>();
            services.AddScoped<IImageMetadataRepository, ImageMetadataCosmosDbRepository>();
            services.AddScoped<IImageValidator, ImageValidator>();
            
            services.AddTransient<IImageUploadService, ImageUploadService>();
            services.AddTransient<IImageSearchService, ImageSearchService>();
            services.AddTransient<IImageDeleteService, ImageDeleteService>();
            
            services.AddHttpClient<ImageDownloadClient>()
                .AddTransientHttpErrorPolicy(p =>
                    p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)));
            
            services.AddMediatR(typeof(Startup).Assembly, 
                typeof(UploadImageCommand).Assembly,
                typeof(UploadImageCommandHandler).Assembly);  
            
            services.AddFluentValidation(fv =>
            {
                fv.RegisterValidatorsFromAssemblyContaining<Startup>();
                fv.ValidatorFactoryType = typeof(HttpContextServiceProviderValidatorFactory);
            });
            services.AddFluentValidationRulesToSwagger();
            
            services.AddHealthChecks(Configuration);
            services.SetupAppInsights(Configuration);
            
            services.AddMassTransit(x =>
            {
                x.AddConsumers(Assembly.GetExecutingAssembly());
                x.UsingAzureServiceBus((context, cfg) =>
                {
                    var configuration = context.GetRequiredService<IConfiguration>();
                    var serviceBusConnectionString = configuration.GetValue<string>("ServiceBus:ConnectionString");
                    cfg.Host(serviceBusConnectionString);

                    var topicsConfiguration = context.GetRequiredService<IOptions<TopicsConfiguration>>();

                    cfg.Message<OriginalImageUploadedIntegrationEvent>(c => 
                        c.SetEntityName(topicsConfiguration.Value.UploadedImagesTopicName));
                    cfg.Message<ImageMetadataDeletedIntegrationEvent>(c => 
                        c.SetEntityName(topicsConfiguration.Value.DeletedImagesTopicName));

                    cfg.MaxConcurrentCalls = topicsConfiguration.Value.MaxConcurrentCalls;
                    
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
            services.AddMassTransitHostedService();
        }

        private void RegisterConfigurations(IServiceCollection services)
        {
            services.Configure<ImageLimitationSettings>(Configuration.GetSection("ImageLimitations"));
            services.Configure<ImageAzureStorageSettings>(Configuration.GetSection("AzureStorage"));
            services.Configure<ImageMetadataCosmosDbStorageSettings>(Configuration.GetSection("CosmosDb"));
            services.Configure<TopicsConfiguration>(Configuration.GetSection("Topics"));
            services.Configure<CdnSettings>(Configuration.GetSection("Cdn"));
            services.Configure<ImageThumbnailsSettings>(Configuration.GetSection("Thumbnails"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                app.UseSwaggerUI(_ =>
                    {
                        c.SwaggerEndpoint($"/swagger/{CurrentVersion}/swagger.json", ApiTitle);
                        c.RoutePrefix = string.Empty;
                    }
                );
            });

            app.UseMiddleware<ServiceFabricResourceNotFoundMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });
        }
    }
 
    public static class CustomExtensionsMethods
    {
        public static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());
            
            var cosmosDbConnectionString = configuration["CosmosDb:ConnectionString"];
            var cosmosDbDatabase = configuration["CosmosDb:DatabaseId"];
            hcBuilder.AddCosmosDb(cosmosDbConnectionString, cosmosDbDatabase, "ik.imager-cosmossdb-check", tags: new [] { "cosmosdb" });

            var azureConnectionString = configuration["AzureStorage:ConnectionString"];
            var azureContainerName = configuration["AzureStorage:ImagesContainerName"];
            hcBuilder.AddAzureBlobStorage(azureConnectionString, azureContainerName, name: "ik.imager-blobstorage-check", tags: new [] { "blobstorage" });
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