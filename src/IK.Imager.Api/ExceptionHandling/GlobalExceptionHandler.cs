using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Repositories;
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
/// <see cref="ValidationException"/> the core handlers throw, a 409 when an image id is already taken,
/// a 500 with a generic message for anything else.
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
    private const string ConflictTitle = "The image id is already in use.";
    private const string ConflictDetail =
        "An image with this id already exists in this tenant. Ids are unique per tenant, whatever collection "
        + "an image is in. Delete the existing image first, or upload under a different id.";

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ValidationException)
        {
            _logger.RequestRejected(httpContext.Request.Path, exception.Message);

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

        //the id is the caller's to choose, so a clash is their error rather than a fault - and it has to be
        //distinguishable from a malformed request, since retrying with the same id will never work
        if (exception is ImageAlreadyExistsException)
        {
            _logger.RequestRejected(httpContext.Request.Path, exception.Message);

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Instance = httpContext.Request.Path,
                Status = StatusCodes.Status409Conflict,
                Title = ConflictTitle,
                Detail = ConflictDetail
            }, cancellationToken);
            return true;
        }

        _logger.UnhandledException(exception, httpContext.Request.Path);

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
