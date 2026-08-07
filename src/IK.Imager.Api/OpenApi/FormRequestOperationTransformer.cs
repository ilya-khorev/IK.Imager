using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IK.Imager.Api.OpenApi;

/// <summary>
/// Restores what a form-bound request model loses on its way into the document.
///
/// A [FromForm] model never reaches a schema transformer as a type: ASP.NET Core flattens it into one field
/// per property and builds the request body schema from the resulting parameter descriptions. Neither the
/// <see cref="FluentValidationRules"/> nor the XML summary of a property survives that, so both are matched
/// back onto the fields through the model metadata of those parameters.
/// </summary>
internal sealed class FormRequestOperationTransformer : IOpenApiOperationTransformer
{
    private readonly XmlPropertyDescriptions _propertyDescriptions;

    public FormRequestOperationTransformer(XmlPropertyDescriptions propertyDescriptions)
    {
        _propertyDescriptions = propertyDescriptions;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var formSchemas = GetFormSchemas(operation);
        if (formSchemas.Count == 0)
            return Task.CompletedTask;

        //one form maps to one model, but the rules are cached per container type anyway - a model can
        //contribute fields from more than one level of its hierarchy
        var rulesByContainer = new Dictionary<Type, Dictionary<string, List<IPropertyValidator>>?>();
        var appliedDescriptions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameter in context.Description.ParameterDescriptions)
        {
            if (parameter.Source != BindingSource.Form && parameter.Source != BindingSource.FormFile)
                continue;

            if (parameter.ModelMetadata?.ContainerType is not {} containerType || parameter.ModelMetadata.PropertyName is not {} propertyName)
                continue;

            if (!rulesByContainer.TryGetValue(containerType, out var rulesByMember))
                rulesByContainer[containerType] = rulesByMember = FluentValidationRules.ForType(context.ApplicationServices, containerType);

            List<IPropertyValidator>? propertyValidators = null;
            rulesByMember?.TryGetValue(propertyName, out propertyValidators);

            var description = _propertyDescriptions.ForProperty(containerType, propertyName);

            foreach (var formSchema in formSchemas)
            {
                if (formSchema.Properties is not {} formProperties || !formProperties.TryGetValue(parameter.Name, out var fieldSchema))
                    continue;

                if (propertyValidators != null)
                    foreach (var propertyValidator in propertyValidators)
                        FluentValidationRules.Apply(propertyValidator, formSchema, fieldSchema as OpenApiSchema, parameter.Name);

                if (description != null && TrySetDescription(fieldSchema, description))
                    appliedDescriptions.Add(description);
            }
        }

        //having nowhere to put the summary of a flattened property, ASP.NET Core falls back to describing
        //the request body with it - now that it sits on the field it was written for, drop the duplicate
        if (operation.RequestBody?.Description is {} bodyDescription && appliedDescriptions.Contains(bodyDescription))
            operation.RequestBody.Description = null;

        return Task.CompletedTask;
    }

    private static bool TrySetDescription(IOpenApiSchema fieldSchema, string description)
    {
        //a field whose type is documented in the components section is a reference rather than a schema,
        //and carries its own description alongside the $ref
        switch (fieldSchema)
        {
            case OpenApiSchema schema when string.IsNullOrEmpty(schema.Description):
                schema.Description = description;
                return true;

            case OpenApiSchemaReference reference when string.IsNullOrEmpty(reference.Description):
                reference.Description = description;
                return true;

            default:
                return false;
        }
    }

    private static List<OpenApiSchema> GetFormSchemas(OpenApiOperation operation)
    {
        var formSchemas = new List<OpenApiSchema>();

        if (operation.RequestBody?.Content == null)
            return formSchemas;

        foreach (var (mediaType, content) in operation.RequestBody.Content)
        {
            if (mediaType is not ("multipart/form-data" or "application/x-www-form-urlencoded"))
                continue;

            if (content.Schema is OpenApiSchema {Properties.Count: > 0} formSchema)
                formSchemas.Add(formSchema);
        }

        return formSchemas;
    }
}
