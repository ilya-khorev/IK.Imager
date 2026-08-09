using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Api.ExceptionHandling;

/// <summary>
/// Turns an unhandled exception into a response: a 400 ValidationProblemDetails for the
/// <see cref="ValidationException"/> the core handlers throw, a 500 with a generic message for anything else.
///
/// Replaces the MVC exception filter this service used before minimal APIs - an endpoint filter cannot see
/// an exception thrown by another filter, so the handling belongs in the pipeline instead.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;

    private const string ValidationDetail = "Please refer to the errors property for additional details.";
    private const string ValidationErrorKey = "ModelValidation";
    private const string GenericErrorMessage = "Error occured. Please try again later.";

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(new EventId(exception.HResult), exception, exception.Message);

        if (exception is ValidationException)
        {
            var problemDetails = new ValidationProblemDetails
            {
                Instance = httpContext.Request.Path,
                Status = StatusCodes.Status400BadRequest,
                Detail = ValidationDetail
            };

            problemDetails.Errors.Add(ValidationErrorKey, [exception.Message]);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        var error = new JsonErrorResponse
        {
            Messages = [GenericErrorMessage],
            DeveloperMessage = _env.IsDevelopment() ? exception.ToString() : null
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}

public class JsonErrorResponse
{
    /// <summary>
    /// Error messages list
    /// </summary>
    public string[] Messages { get; set; } = [];

    /// <summary>
    /// Debug information (inner exception). Only populated in the Development environment.
    /// </summary>
    public string? DeveloperMessage { get; set; }
}
