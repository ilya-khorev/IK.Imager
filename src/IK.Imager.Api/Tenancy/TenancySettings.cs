#pragma warning disable 1591
namespace IK.Imager.Api.Tenancy;

public class TenancySettings
{
    /// <summary>
    /// Where the tenant is read from: <c>Header</c> or <c>Claim</c>.
    /// An unrecognised value stops the service from starting.
    /// </summary>
    public string Source { get; set; } = TenantSources.Header;

    /// <summary>
    /// Request header carrying the tenant. Used when <see cref="Source"/> is <c>Header</c>.
    /// </summary>
    public string HeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// Claim carrying the tenant. Used when <see cref="Source"/> is <c>Claim</c>.
    /// Every identity provider names this differently - Entra ID uses 'tid', Auth0 uses 'org_id' - so it
    /// is configuration rather than code.
    /// </summary>
    public string ClaimType { get; set; } = string.Empty;
}

public static class TenantSources
{
    public const string Header = "Header";
    public const string Claim = "Claim";
}
