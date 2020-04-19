using System;
using System.IO;
using System.Reflection;
using AutoMapper;
using IK.Imager.Api.Filters;
using IK.Imager.Api.Services;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Services;
using IK.Imager.Core.Configuration;
using IK.Imager.Core.Services;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.EventBus.AzureServiceBus;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.IntegrationEvents;
using IK.Imager.Storage.Abstractions.Storage;
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

            //todo sort endpoints in swagger

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(CurrentVersion, new OpenApiInfo {Title = ApiTitle, Version = CurrentVersion});
                c.IncludeXmlComments(XmlCommentsFilePath);
            });

            services.AddAutoMapper(c => c.AddProfile<AutoMapping>(), typeof(Startup));

            RegisterConfigurations(services);

            services.AddSingleton<IEventBus, ServiceBus>();
            services.AddSingleton<IImageMetadataStorage, ImageMetadataCosmosDbStorage>();
            services.AddSingleton<IImageBlobStorage, ImageBlobAzureStorage>();
            services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
            services.AddSingleton<IImageIdentifierProvider, ImageIdentifierProvider>();

            services.AddSingleton<IImageUploadService, ImageUploadService>();
            services.AddSingleton<IImageSearchService, ImageSearchService>();
            services.AddSingleton<IImageDeleteService, ImageDeleteService>();

            services.AddHttpClient<ImageDownloadClient>()
                .AddTransientHttpErrorPolicy(p =>
                    p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)));

            services.AddHealthChecks(); //todo
            SetupAppInsights(services);
        }

        private void RegisterConfigurations(IServiceCollection services)
        {
            services.Configure<ImageLimitationSettings>(Configuration.GetSection("ImageLimitations"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ImageLimitationSettings>>().Value);

            services.Configure<ServiceBusSettings>(Configuration.GetSection("ServiceBus"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ServiceBusSettings>>().Value);

            services.Configure<ImageAzureStorageConfiguration>(Configuration.GetSection("AzureStorage"));
            services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<ImageAzureStorageConfiguration>>().Value);

            services.Configure<ImageMetadataCosmosDbStorageConfiguration>(Configuration.GetSection("CosmosDb"));
            services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<ImageMetadataCosmosDbStorageConfiguration>>().Value);

            services.Configure<TopicsConfiguration>(Configuration.GetSection("Topics"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<TopicsConfiguration>>().Value);
        }

        private void SetupAppInsights(IServiceCollection services)
        {
            ApplicationInsightsServiceOptions aiOptions = new ApplicationInsightsServiceOptions();
            var appInsightsDependencyConfigValue =
                Configuration.GetValue<bool>("ApplicationInsights:EnableDependencyTrackingTelemetryModule");
            //dependency tracking is disabled by default as it is quite expensive
            aiOptions.EnableDependencyTrackingTelemetryModule = appInsightsDependencyConfigValue;

            //by default instrumentation key is taken from config
            //Alternatively, specify the instrumentation key in either of the following environment variables.
            //APPINSIGHTS_INSTRUMENTATIONKEY or ApplicationInsights:InstrumentationKey
            services.AddApplicationInsightsTelemetry(aiOptions);

            var appInsightsAuthApiKey = Configuration.GetValue<string>("ApplicationInsights:AuthenticationApiKey");
            if (!string.IsNullOrWhiteSpace(appInsightsAuthApiKey))
                services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) =>
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

            app.UseSwagger(c => { c.SerializeAsV2 = true; });
            app.UseSwaggerUI(c =>
            {
                app.UseSwaggerUI(
                    options =>
                    {
                        c.SwaggerEndpoint($"/swagger/{CurrentVersion}/swagger.json", ApiTitle);
                        c.RoutePrefix = string.Empty;
                    }
                );
            });

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}