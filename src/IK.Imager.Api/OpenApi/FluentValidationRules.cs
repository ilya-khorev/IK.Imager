using System;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.OpenApi;

namespace IK.Imager.Api.OpenApi;

/// <summary>
/// Translates the constraints declared by the FluentValidation validators in IK.Imager.Api/Validations into
/// their OpenAPI equivalents, so that the documented shape of a request matches what
/// <see cref="Filters.FluentValidationActionFilter"/> actually enforces.
///
/// This replaces MicroElements.Swashbuckle.FluentValidation, which only plugs into Swashbuckle's own schema
/// generator and therefore cannot be used now that the document is produced by ASP.NET Core. Only the rule
/// kinds the validators use are translated - length, "not empty"/"not null", and regular expressions;
/// anything expressed as a <c>Must(...)</c> predicate is invisible here, exactly as it was to MicroElements.
/// </summary>
internal static class FluentValidationRules
{
    /// <summary>
    /// Returns the property validators registered for <paramref name="modelType"/>, keyed by the CLR member
    /// name they were declared against, or null when the type has no validator.
    /// </summary>
    public static Dictionary<string, List<IPropertyValidator>>? ForType(IServiceProvider services, Type modelType)
    {
        if (services.GetService(typeof(IValidator<>).MakeGenericType(modelType)) is not IValidator validator)
            return null;

        var rulesByMember = new Dictionary<string, List<IPropertyValidator>>(StringComparer.Ordinal);

        foreach (var rule in validator.CreateDescriptor().Rules)
        {
            if (rule.Member == null)
                continue;

            if (!rulesByMember.TryGetValue(rule.Member.Name, out var propertyValidators))
                rulesByMember[rule.Member.Name] = propertyValidators = [];

            foreach (var component in rule.Components)
                propertyValidators.Add(component.Validator);
        }

        return rulesByMember;
    }

    /// <summary>
    /// Applies a single property validator to the schema of the object that declares the property
    /// (<paramref name="containerSchema"/>, which carries the required list) and to the schema of the
    /// property itself. <paramref name="propertyName"/> is the name the property has in the document,
    /// which is not necessarily its CLR member name.
    /// </summary>
    public static void Apply(IPropertyValidator propertyValidator, OpenApiSchema containerSchema, OpenApiSchema? propertySchema, string propertyName)
    {
        switch (propertyValidator)
        {
            //NotEmptyValidator implements both marker interfaces, so this case has to come first
            case INotEmptyValidator:
                Require(containerSchema, propertyName);
                //"required" only says the member is present - the empty value has to be excluded separately
                if (propertySchema?.Type is {} notEmptyType)
                {
                    if (notEmptyType.HasFlag(JsonSchemaType.Array))
                        propertySchema.MinItems = Math.Max(propertySchema.MinItems ?? 0, 1);
                    else if (notEmptyType.HasFlag(JsonSchemaType.String))
                        propertySchema.MinLength = Math.Max(propertySchema.MinLength ?? 0, 1);
                }
                break;

            case INotNullValidator:
                Require(containerSchema, propertyName);
                break;

            //covers Length, MinimumLength and MaximumLength - the unset bound is left at its default
            case ILengthValidator lengthValidator when propertySchema != null:
                if (lengthValidator.Min > 0)
                    propertySchema.MinLength = Math.Max(propertySchema.MinLength ?? 0, lengthValidator.Min);
                if (lengthValidator.Max > 0)
                    propertySchema.MaxLength = lengthValidator.Max;
                break;

            case IRegularExpressionValidator regularExpressionValidator when propertySchema != null:
                propertySchema.Pattern = regularExpressionValidator.Expression;
                break;
        }
    }

    private static void Require(OpenApiSchema containerSchema, string propertyName) =>
        (containerSchema.Required ??= new HashSet<string>(StringComparer.Ordinal)).Add(propertyName);
}
