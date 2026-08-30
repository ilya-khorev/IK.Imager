using System.Linq;
using IK.Imager.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Xunit;

namespace IK.Imager.Api.Tests.Extensions;

/// <summary>
/// The Telemetry:ConnectionString gate, and the container validation Program.cs turns on in every
/// environment. UseAzureMonitor throws when it finds no connection string, so a deployment without one
/// has to skip the whole pipeline rather than pass an empty value through.
/// </summary>
public class ObservabilityExtensionsTests
{
    private const string FakeConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost/;LiveEndpoint=https://localhost/";

    private static IConfiguration Configuration(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(x => x.Key, x => x.Value))
            .Build();

    private static ServiceProvider Build(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddObservability(configuration);

        //what Program.cs asks for, so a captive dependency in the telemetry pipeline fails here
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    //the distro registers its ILoggerProvider through a factory, so there is no implementation type to
    //match on - but AddLogging by itself registers no provider at all, so the presence of one is the answer
    private static bool HasOpenTelemetryLogging(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddObservability(configuration);

        return services.Any(x => x.ServiceType == typeof(ILoggerProvider)) &&
               services.Any(x => x.ServiceType == typeof(TracerProvider));
    }

    [Fact]
    public void AddObservability_NoConnectionString_DoesNotRegisterOpenTelemetryLogging()
    {
        Assert.False(HasOpenTelemetryLogging(Configuration(("Telemetry:ConnectionString", string.Empty))));
    }

    [Fact]
    public void AddObservability_NoConfigurationAtAll_StillBuilds()
    {
        using var provider = Build(Configuration());

        Assert.NotNull(provider.GetRequiredService<ILoggerFactory>());
    }

    /// <summary>
    /// The distro reads this variable on its own, so the gate has to consider it or a machine that has it
    /// set would be skipped here and export anyway.
    /// </summary>
    [Fact]
    public void AddObservability_ConnectionStringFromTheEnvironmentVariableName_RegistersOpenTelemetryLogging()
    {
        Assert.True(HasOpenTelemetryLogging(
            Configuration(("APPLICATIONINSIGHTS_CONNECTION_STRING", FakeConnectionString))));
    }

    [Fact]
    public void AddObservability_ConnectionStringConfigured_RegistersOpenTelemetryLogging()
    {
        Assert.True(HasOpenTelemetryLogging(Configuration(("Telemetry:ConnectionString", FakeConnectionString))));
    }

    [Fact]
    public void AddObservability_ConnectionStringConfigured_ContainerStillValidates()
    {
        using var provider = Build(Configuration(("Telemetry:ConnectionString", FakeConnectionString)));

        Assert.NotNull(provider.GetRequiredService<ILoggerFactory>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddObservability_DependencyTracingEitherWay_ContainerStillValidates(bool enabled)
    {
        using var provider = Build(Configuration(
            ("Telemetry:ConnectionString", FakeConnectionString),
            ("Telemetry:EnableDependencyTracing", enabled.ToString())));

        Assert.NotNull(provider.GetRequiredService<ILoggerFactory>());
    }
}
