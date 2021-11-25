using System;
using System.IO;
using System.Reflection;
using IK.Imager.Api.Filters;
using IK.Imager.Api.Handlers;
using IK.Imager.Api.Middleware;
using IK.Imager.Api.Services;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Thumbnails;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Thumbnails;
using IK.Imager.Core.Validation;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.IntegrationEvents;
using IK.Imager.Storage.Abstractions.Repositories;
using MassTransit;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Polly;

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
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(CurrentVersion, new OpenApiInfo {Title = ApiTitle, Version = CurrentVersion});
                c.IncludeXmlComments(XmlCommentsFilePath);
            });

            services.AddAutoMapper(c => c.AddProfile<MappingProfile>(), typeof(Startup));

            RegisterConfigurations(services);
            
            services.AddSingleton<ICosmosDbClient, CosmosDbClient>();
            services.AddSingleton<IImageMetadataRepository, ImageMetadataCosmosDbRepository>(); //todo scoped / transient
            services.AddSingleton<IAzureBlobClient, AzureBlobClient>(s =>
            {
                var settings = s.GetRequiredService<IOptions<ImageAzureStorageSettings>>();
                return new AzureBlobClient(settings.Value.ConnectionString);
            });
                
            services.AddSingleton<IImageBlobRepository, ImageBlobAzureRepository>(); //todo scoped / transient
            services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
            services.AddSingleton<IImageIdentifierProvider, ImageIdentifierProvider>();
            services.AddSingleton<ICdnService, CdnService>();
            services.AddSingleton<IImageResizing, ImageResizing>();

            services.AddScoped<IImageValidator, ImageValidator>();
            services.AddScoped<IImageUploadService, ImageUploadService>();
            services.AddScoped<IImageSearchService, ImageSearchService>();
            
            services.AddTransient<IImageDeleteService, ImageDeleteService>();
            services.AddTransient<IImageThumbnailService, ImageThumbnailService>();
            
            services.AddHttpClient<ImageDownloadClient>()
                .AddTransientHttpErrorPolicy(p =>
                    p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)));

            services.AddHealthChecks(); //todo add hc for cosmosdb, blobstorage, servicebus
            SetupAppInsights(services);

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
                    cfg.Message<ImageDeletedIntegrationEvent>(c => 
                        c.SetEntityName(topicsConfiguration.Value.DeletedImagesTopicName));
                    
                    cfg.SubscriptionEndpoint<OriginalImageUploadedIntegrationEvent>(topicsConfiguration.Value.SubscriptionName,
                        configurator =>
                        {
                            configurator.ConfigureConsumer<GenerateThumbnailsHandler>(context);
                        });
                    cfg.SubscriptionEndpoint<ImageDeletedIntegrationEvent>(topicsConfiguration.Value.SubscriptionName,
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

        private void SetupAppInsights(IServiceCollection services)
        {
            ApplicationInsightsServiceOptions aiOptions = new ApplicationInsightsServiceOptions();
            var appInsightsDependencyConfigValue =
                Configuration.GetValue<bool>("ApplicationInsights:EnableDependencyTrackingTelemetryModule");
            //dependency tracking is disabled by default as it is produce a lot of logs and therefore quite expensive
            aiOptions.EnableDependencyTrackingTelemetryModule = appInsightsDependencyConfigValue;

            //By default, instrumentation key is taken from the configuration
            //Alternatively, specify the instrumentation key in either of the following environment variables:
            //APPINSIGHTS_INSTRUMENTATIONKEY or ApplicationInsights:InstrumentationKey
            services.AddApplicationInsightsTelemetry(aiOptions);

            var appInsightsAuthApiKey = Configuration.GetValue<string>("ApplicationInsights:AuthenticationApiKey");
            if (!string.IsNullOrWhiteSpace(appInsightsAuthApiKey))
                services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, _) =>
                    module.AuthenticationApiKey = appInsightsAuthApiKey);
        }

        private string XmlCommentsFilePath
        {
            get
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                return xmlPath;
            }
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

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            
            app.UseMiddleware<ServiceFabricResourceNotFoundMiddleware>();
        }
    }
}