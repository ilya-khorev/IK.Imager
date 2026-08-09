using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

#pragma warning disable 1591

namespace IK.Imager.Api.Validation;

public static class ValidationFilterExtensions
{
    /// <summary>
    /// Validates the <typeparamref name="T"/> argument of this endpoint with its registered FluentValidation
    /// validator and declares the resulting 400 in the OpenAPI document.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationEndpointFilter<T>>()
            .ProducesValidationProblem();
}
