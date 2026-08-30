using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace IK.Imager.Api.Tenancy;

/// <summary>
/// Reads the tenant from a claim on the authenticated principal, which is where it belongs once the service
/// has an identity provider in front of it - a tenant the caller states is a tenant the caller can change.
///
/// Needs an authentication scheme registered through the hook on
/// <see cref="Extensions.ApiServiceCollectionExtensions.AddApiServices"/>; with no scheme the principal
/// carries no claims and every request is rejected.
/// </summary>
public class ClaimsTenantResolver(IOptions<TenancySettings> settings) : ITenantResolver
{
    private readonly string _claimType = settings.Value.ClaimType;

    /// <inheritdoc />
    public string Source => $"the '{_claimType}' claim";

    /// <inheritdoc />
    public string? Resolve(HttpContext httpContext) => httpContext.User.FindFirst(_claimType)?.Value;
}
