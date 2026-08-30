using System;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Api.Tenancy;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace IK.Imager.Api.OpenApi;

/// <summary>
/// Declares the tenant header on every image operation.
///
/// The tenant is not part of any request model, so nothing else in the document would mention it and a
/// caller reading the Swagger UI would get a 400 with no idea why. Only the /images group requires one -
/// the health endpoints are outside it.
/// </summary>
internal sealed class TenantHeaderOperationTransformer(IOptions<TenancySettings> settings) : IOpenApiOperationTransformer
{
    private const string ImagesPath = "images";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        //only meaningful while the tenant is read from a header; a claim is carried by the token instead
        if (!string.Equals(settings.Value.Source, TenantSources.Header, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var path = context.Description.RelativePath;
        if (path == null || !path.StartsWith(ImagesPath, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = settings.Value.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Tenant that owns the images. Image ids are unique within a tenant, and the tenant "
                          + "is the first segment of every image url.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });

        return Task.CompletedTask;
    }
}
