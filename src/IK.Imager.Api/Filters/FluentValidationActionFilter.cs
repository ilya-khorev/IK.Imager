using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

#pragma warning disable 1591

namespace IK.Imager.Api.Filters;

/// <summary>
/// Runs the registered FluentValidation validator (if any) against every action argument and short-circuits
/// with a 400 ValidationProblemDetails when the model is invalid.
///
/// FluentValidation dropped its built-in ASP.NET Core auto-validation, so this filter takes over what
/// AddFluentValidation used to do. Validators themselves stay in IK.Imager.Api/Validations.
/// </summary>
public class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value == null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.Value.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argument.Value.GetType());
            // CreateInstance only returns null for Nullable<T>; validationContextType is always a class here.
            var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument.Value)!;

            var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (validationResult.IsValid)
                continue;

            foreach (var error in validationResult.Errors)
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Instance = context.HttpContext.Request.Path,
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        await next();
    }
}
