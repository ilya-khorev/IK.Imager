using System.Text.Json;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using HealthChecks.CosmosDb;
using HealthChecks.UI.Client;
using IK.Imager.Storage.AzureBlobs;
using IK.Imager.Storage.CosmosDb;
using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;

#pragma warning disable 1591

namespace IK.Imager.Api.Extensions;

public static class ObservabilityExtensions
{
    /// <summary>
    /// The configuration section every telemetry setting is bound from. The values are the settable
    /// properties of AzureMonitorOptions, so there is no settings class of ours to hold the constant.
    /// </summary>
    public const string TelemetrySectionName = "Telemetry";

    public const string TelemetryConnectionStringPath = "Telemetry:ConnectionString";

    /// <summary>
    /// What the Azure Monitor distro reads on its own when nothing is configured.
    /// </summary>
    private const string ConnectionStringVariable = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Off by default, which is what the Application Insights deployment did: the module it replaces was
    /// switched off because "it produces a lot of logs and is therefore quite expensive". Turning it on
    /// exports a client span for every blob call, CDN purge and image download.
    /// </summary>
    private const string DependencyTracingPath = "Telemetry:EnableDependencyTracing";

    /// <summary>
    /// Replaces the default logging providers with a single json console.
    /// </summary>
    /// <remarks>
    /// Only the console here. The OpenTelemetry provider is registered later, by AddObservability - it has
    /// to be after this, because ClearProviders drops whatever is registered at the moment it runs.
    /// </remarks>
    public static ILoggingBuilder AddImagerLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffZ";
            //one object per line, which is what a log shipper can read
            options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
        });

        //puts the trace id on every console line, so the console and Azure Monitor name the same operation
        logging.Configure(options => options.ActivityTrackingOptions =
            ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId);

        return logging;
    }

    /// <summary>
    /// Registers the health checks and the OpenTelemetry pipeline.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddImagerHealthChecks();
        services.AddImagerTelemetry(configuration);

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
            s => new CosmosClient(s.GetRequiredService<IOptions<CosmosDbSettings>>().Value.ConnectionString),
            s => new AzureCosmosDbHealthCheckOptions
            {
                DatabaseId = s.GetRequiredService<IOptions<CosmosDbSettings>>().Value.DatabaseId
            },
            "ik.imager-cosmossdb-check", tags: new[] { "cosmosdb" });

        hcBuilder.AddAzureBlobStorage(
            s => new BlobServiceClient(s.GetRequiredService<IOptions<AzureBlobStorageSettings>>().Value.ConnectionString),
            s => new AzureBlobStorageHealthCheckOptions
            {
                //lowercased to match AzureBlobImageRepository, which lowercases before creating the container
                ContainerName = s.GetRequiredService<IOptions<AzureBlobStorageSettings>>().Value.ImagesContainerName.ToLowerInvariant()
            },
            "ik.imager-blobstorage-check", tags: new[] { "blobstorage" });

        return services;
    }

    //A deployment without a connection string keeps the json console and nothing else. UseAzureMonitor
    //throws rather than degrading, so it has to be skipped entirely - and the distro reads the environment
    //variable on its own, so both sources are checked or the gate is wrong in one direction or the other.
    private static IServiceCollection AddImagerTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>(TelemetryConnectionStringPath);
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = configuration.GetValue<string>(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
            return services;

        //off by default in the distro, and it is what carries the ImageId scope onto an exported log record
        services.Configure<OpenTelemetryLoggerOptions>(options =>
        {
            options.IncludeScopes = true;
            options.ParseStateValues = true;
        });

        //the health endpoints are probed continuously and say nothing a trace can use
        services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
            options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/hc") &&
                !context.Request.Path.StartsWithSegments("/liveness"));

        //the distro always installs HttpClient instrumentation, so this is the only way to keep the
        //deployment's existing behaviour rather than silently changing what it costs
        if (!configuration.GetValue<bool>(DependencyTracingPath))
            services.Configure<HttpClientTraceInstrumentationOptions>(options =>
                options.FilterHttpRequestMessage = _ => false);

        services.AddOpenTelemetry()
            //load-bearing beyond exporting: MassTransit writes the trace context into the message
            //(the MT-Activity-Id header) only while something listens to this source, so without the
            //AddSource the consumer starts with no Activity and the bus hop breaks the trace
            .WithTracing(tracing => tracing
                .AddSource(DiagnosticHeaders.DefaultListenerName))
            .WithMetrics(metrics => metrics
                .AddMeter(InstrumentationOptions.MeterName))
            .UseAzureMonitor(options =>
            {
                configuration.GetSection(TelemetrySectionName).Bind(options);
                options.ConnectionString = connectionString;
            });

        return services;
    }
}
