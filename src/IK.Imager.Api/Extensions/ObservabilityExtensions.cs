using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using HealthChecks.CosmosDb;
using HealthChecks.UI.Client;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Replaces the default logging providers with a single systemd-formatted console.
    /// </summary>
    public static ILoggingBuilder AddImagerLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddSystemdConsole(options => options.TimestampFormat = "[dd-MM-yyyy HH:mm:ss.fff] ");

        return logging;
    }

    /// <summary>
    /// Registers the health checks and Application Insights telemetry.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddImagerHealthChecks();
        services.AddImagerApplicationInsights(configuration);

        return services;
    }

    /// <summary>
    /// Maps the readiness (/hc, every check) and liveness (/liveness, self only) endpoints.
    /// </summary>
    public static WebApplication MapImagerHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/hc", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecks("/liveness", new HealthCheckOptions
        {
            Predicate = r => r.Name.Contains("self")
        });

        return app;
    }

    //The storage settings are read through IOptions rather than straight off IConfiguration so that the
    //probes can never target a different database or container than the repositories do.
    private static IServiceCollection AddImagerHealthChecks(this IServiceCollection services)
    {
        var hcBuilder = services.AddHealthChecks();

        hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());

        hcBuilder.AddAzureCosmosDB(
            s => new CosmosClient(s.GetRequiredService<IOptions<ImageMetadataCosmosDbStorageSettings>>().Value.ConnectionString),
            s => new AzureCosmosDbHealthCheckOptions
            {
                DatabaseId = s.GetRequiredService<IOptions<ImageMetadataCosmosDbStorageSettings>>().Value.DatabaseId
            },
            "ik.imager-cosmossdb-check", tags: new[] { "cosmosdb" });

        hcBuilder.AddAzureBlobStorage(
            s => new BlobServiceClient(s.GetRequiredService<IOptions<ImageAzureStorageSettings>>().Value.ConnectionString),
            s => new AzureBlobStorageHealthCheckOptions
            {
                //lowercased to match ImageBlobAzureRepository, which lowercases before creating the container
                ContainerName = s.GetRequiredService<IOptions<ImageAzureStorageSettings>>().Value.ImagesContainerName.ToLowerInvariant()
            },
            "ik.imager-blobstorage-check", tags: new[] { "blobstorage" });

        return services;
    }

    private static IServiceCollection AddImagerApplicationInsights(this IServiceCollection services, IConfiguration configuration)
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

        return services;
    }
}
