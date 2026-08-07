using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IK.Imager.Api.OpenApi;

/// <summary>
/// Applies <see cref="FluentValidationRules"/> to the schemas generated for the request models that are
/// bound from the body. Form-bound models are flattened into individual fields before they reach a schema
/// transformer and are handled by <see cref="FluentValidationOperationTransformer"/> instead.
/// </summary>
internal sealed class FluentValidationSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        //the transformer is invoked for every schema, including the one generated for each individual
        //property - properties are handled from their declaring type, so those calls are skipped
        if (context.JsonPropertyInfo != null || schema.Properties is not {Count: > 0})
            return Task.CompletedTask;

        var rulesByMember = FluentValidationRules.ForType(context.ApplicationServices, context.JsonTypeInfo.Type);
        if (rulesByMember == null)
            return Task.CompletedTask;

        foreach (var property in context.JsonTypeInfo.Properties)
        {
            //FluentValidation keys its rules by the CLR member name, while the schema is keyed by the
            //serialized name - AttributeProvider is the PropertyInfo the JSON property was built from
            var memberName = (property.AttributeProvider as MemberInfo)?.Name;
            if (memberName == null || !rulesByMember.TryGetValue(memberName, out var propertyValidators))
                continue;

            if (!schema.Properties.TryGetValue(property.Name, out var propertySchema))
                continue;

            foreach (var propertyValidator in propertyValidators)
                FluentValidationRules.Apply(propertyValidator, schema, propertySchema as OpenApiSchema, property.Name);
        }

        return Task.CompletedTask;
    }
}
