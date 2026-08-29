using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Api.Validation;
using Microsoft.AspNetCore.Http;

namespace IK.Imager.Api.Tenancy;

/// <summary>
/// Resolves the tenant of the request and rejects the request with a 400 when it carries none, or one that
/// could not be part of a url and a partition key.
///
/// An endpoint filter rather than middleware, so that it applies to the /images group alone and the health
/// endpoints stay reachable without a tenant. It is attached once, in
/// <see cref="Features.ImageEndpoints.MapImageEndpoints"/>.
/// </summary>
internal sealed class TenantEndpointFilter(ITenantResolver resolver, TenantContext tenantContext) : IEndpointFilter
{
    private const string Key = "TenantId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var tenantId = resolver.Resolve(context.HttpContext);

        if (string.IsNullOrWhiteSpace(tenantId))
            return Reject(context, $"The tenant is required. Pass it in {resolver.Source}.");

        if (tenantId.Length > IdentifierConstraints.MaxTenantIdLength)
            return Reject(context,
                $"The tenant must be at most {IdentifierConstraints.MaxTenantIdLength} characters long.");

        if (!IdentifierConstraints.IsWellFormed(tenantId))
            return Reject(context,
                "The tenant must be lowercase letters, digits, and dots, underscores or hyphens between them.");

        tenantContext.Set(tenantId);

        return await next(context);
    }

    private static object Reject(EndpointFilterInvocationContext context, string error) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [Key] = [error] },
            instance: context.HttpContext.Request.Path);
}
