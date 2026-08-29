using Microsoft.AspNetCore.Http;

namespace IK.Imager.Api.Tenancy;

/// <summary>
/// Reads the tenant off an incoming request. One implementation per <see cref="TenancySettings.Source"/>.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// The tenant carried by this request, or null when it carries none.
    /// </summary>
    string? Resolve(HttpContext httpContext);

    /// <summary>
    /// Where the tenant was expected, for the error returned when there is none.
    /// </summary>
    string Source { get; }
}
