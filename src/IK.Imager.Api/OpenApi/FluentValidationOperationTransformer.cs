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
/// Applies <see cref="FluentValidationRules"/> to the fields of a form-bound request model.
///
/// A [FromForm] model never reaches a schema transformer as a type: ASP.NET Core flattens it into one field
/// per property and builds the request body schema from the resulting parameter descriptions, so the rules
/// have to be matched back onto it through the model metadata of those parameters.
/// </summary>
internal sealed class FluentValidationOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var formSchemas = GetFormSchemas(operation);
        if (formSchemas.Count == 0)
            return Task.CompletedTask;

        //one form maps to one model, but the rules are cached per container type anyway - a model can
        //contribute fields from more than one level of its hierarchy
        var rulesByContainer = new Dictionary<Type, Dictionary<string, List<IPropertyValidator>>?>();

        foreach (var parameter in context.Description.ParameterDescriptions)
        {
            if (parameter.Source != BindingSource.Form && parameter.Source != BindingSource.FormFile)
                continue;

            if (parameter.ModelMetadata?.ContainerType is not {} containerType || parameter.ModelMetadata.PropertyName is not {} propertyName)
                continue;

            if (!rulesByContainer.TryGetValue(containerType, out var rulesByMember))
                rulesByContainer[containerType] = rulesByMember = FluentValidationRules.ForType(context.ApplicationServices, containerType);

            if (rulesByMember == null || !rulesByMember.TryGetValue(propertyName, out var propertyValidators))
                continue;

            foreach (var formSchema in formSchemas)
            {
                if (formSchema.Properties is not {} formProperties || !formProperties.TryGetValue(parameter.Name, out var fieldSchema))
                    continue;

                foreach (var propertyValidator in propertyValidators)
                    FluentValidationRules.Apply(propertyValidator, formSchema, fieldSchema as OpenApiSchema, parameter.Name);
            }
        }

        return Task.CompletedTask;
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
