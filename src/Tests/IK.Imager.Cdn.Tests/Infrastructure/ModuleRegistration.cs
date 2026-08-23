using System.Collections.Generic;
using System.Net.Http;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IK.Imager.Cdn.Tests.Infrastructure;

/// <summary>
/// Helpers for exercising a module's Add... extension, which is where the base address and the
/// authentication a purger relies on are wired - none of that is visible from the purger itself.
/// </summary>
public static class ModuleRegistration
{
    /// <summary>
    /// Name of the typed client every module registers, i.e. the short name of the service type.
    /// </summary>
    public const string ClientName = nameof(ICdnPurger);

    public static IConfiguration Configuration(params (string Key, string Value)[] settings)
    {
        var values = new Dictionary<string, string?>();
        foreach (var (key, value) in settings)
            values[key] = value;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    public static ServiceProvider BuildProvider(IServiceCollection services)
    {
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// The client the module configured, built the way the typed client is built at runtime.
    /// </summary>
    public static HttpClient CdnHttpClient(ServiceProvider provider) =>
        provider.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);
}
