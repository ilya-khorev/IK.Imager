using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace IK.Imager.Api.Tenancy;

/// <summary>
/// Reads the tenant from a request header.
///
/// This is the unauthenticated mode: the header is not verified against anything, so the tenant is a data
/// partitioning parameter rather than a security boundary, and the service has to be reachable only from
/// callers that are trusted to name a tenant. Switch to <see cref="ClaimsTenantResolver"/> once there is an
/// identity provider to take the tenant from.
/// </summary>
public class HeaderTenantResolver(IOptions<TenancySettings> settings) : ITenantResolver
{
    private readonly string _headerName = settings.Value.HeaderName;

    /// <inheritdoc />
    public string Source => $"the {_headerName} header";

    /// <inheritdoc />
    public string? Resolve(HttpContext httpContext) => httpContext.Request.Headers[_headerName];
}
