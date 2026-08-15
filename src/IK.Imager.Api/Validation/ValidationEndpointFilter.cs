using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace IK.Imager.Api.Validation;

/// <summary>
/// Runs the FluentValidation validator registered for <typeparamref name="T"/> against the argument of that
/// type and short-circuits with a 400 ValidationProblemDetails when the model is invalid.
///
/// FluentValidation dropped its built-in ASP.NET Core auto-validation, so this filter takes over what
/// AddFluentValidation used to do. Attached per endpoint by
/// <see cref="ValidationFilterExtensions.WithValidation{T}"/>; validators live next to the endpoint they guard.
/// </summary>
internal sealed class ValidationEndpointFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationEndpointFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        //a request that failed to bind arrives as a null argument - there is nothing to validate, and the
        //binding failure has already produced its own response
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argument)
            return await next(context);

        var validationResult = await _validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (validationResult.IsValid)
            return await next(context);

        return TypedResults.ValidationProblem(validationResult.ToDictionary(),
            instance: context.HttpContext.Request.Path);
    }
}
