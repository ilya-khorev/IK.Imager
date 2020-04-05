using System;
using System.IO;
using System.Reflection;
using IK.Imager.Api.Configuration;
using IK.Imager.Api.Filters;
using IK.Imager.Api.Services;
using IK.Imager.Core;
using IK.Imager.Core.Abstractions;
using IK.Imager.EventBus.Abstractions;
using IK.Imager.EventBus.AzureServiceBus;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using IK.Imager.ImageBlobStorage.AzureFiles;
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
                c.SwaggerDoc("v1.0", new OpenApiInfo {Title = "IK.Image API", Version = "v1.0"});
                c.IncludeXmlComments(XmlCommentsFilePath);
            });
            
            RegisterConfigurations(services);

            services.AddSingleton<IEventBus, ServiceBus>();
            services.AddSingleton<IImageMetadataStorage, ImageMetadataCosmosDbStorage>();
            services.AddSingleton<IImageBlobStorage, ImageBlobAzureStorage>();
            services.AddSingleton<IImageMetadataReader, ImageMetadataReader>();
            
            services.AddHttpClient<ImageDownloadClient>()
                .AddTransientHttpErrorPolicy(p => 
                    p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600)));
            
            services.AddHealthChecks(); //todo
            SetupAppInsights(services);
        }

        private void RegisterConfigurations(IServiceCollection services)
        {
            services.Configure<ImageLimitationSettings>(Configuration.GetSection("ImageLimitations"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ImageLimitationSettings>>().Value);

            services.Configure<ServiceBusSettings>(Configuration.GetSection("ServiceBus"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ServiceBusSettings>>().Value);
            
            services.Configure<ImageAzureStorageConfiguration>(Configuration.GetSection("ImageBlobsAzureStorage"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ImageAzureStorageConfiguration>>().Value);
            
            services.Configure<ImageMetadataCosmosDbStorageConfiguration>(Configuration.GetSection("ImageMetadataCosmosDbStorage"));
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ImageMetadataCosmosDbStorageConfiguration>>().Value);
        }

        private void SetupAppInsights(IServiceCollection services)
        {
            ApplicationInsightsServiceOptions aiOptions = new ApplicationInsightsServiceOptions();
            var appInsightsDependencyConfigValue =
                Configuration.GetValue<bool>("ApplicationInsights:EnableDependencyTrackingTelemetryModule");
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

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseSwagger(c => { c.SerializeAsV2 = true; });
            app.UseSwaggerUI(c =>
            {
                app.UseSwaggerUI(
                    options =>
                    {
                        c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "IK.Image API");
                        c.RoutePrefix = string.Empty;
                    }
                );
            });
            
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}