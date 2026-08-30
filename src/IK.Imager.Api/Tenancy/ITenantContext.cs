using System;

namespace IK.Imager.Api.Tenancy;

/// <summary>
/// The tenant the current request belongs to.
///
/// This is the single seam between how a tenant is established and everything that uses one. Endpoints read
/// it and pass the value into the core services as an ordinary argument, so nothing below the host knows
/// whether the tenant came from a header, a claim, or anything else.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Tenant of the current request. Always set by the time an endpoint handler runs.
    /// </summary>
    string TenantId { get; }
}

/// <summary>
/// Scoped holder filled in by <see cref="TenantEndpointFilter"/> before the handler runs.
/// </summary>
internal sealed class TenantContext : ITenantContext
{
    private string? _tenantId;

    public string TenantId => _tenantId
        ?? throw new InvalidOperationException(
            $"The tenant has not been resolved. {nameof(TenantEndpointFilter)} must run before the endpoint handler.");

    public void Set(string tenantId) => _tenantId = tenantId;
}
