using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
#pragma warning disable 1591

namespace IK.Imager.Api.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(new EventId(context.Exception.HResult),
            context.Exception,
            context.Exception.Message);

        if (context.Exception.GetType() == typeof(ValidationException))
        {
            var problemDetails = new ValidationProblemDetails
            {
                Instance = context.HttpContext.Request.Path,
                Status = StatusCodes.Status400BadRequest,
                Detail = @"Please refer to the errors property for additional details."
            };

            problemDetails.Errors.Add("ModelValidation", new[] {context.Exception.Message});

            context.Result = new BadRequestObjectResult(problemDetails);
            context.HttpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        }
        else
        {
            var json = new JsonErrorResponse
            {
                Messages = new[] {"Error occured. Please try again later."}
            };

            if (_env.IsDevelopment()) 
                json.DeveloperMessage = context.Exception.ToString();

            context.Result = new BadRequestObjectResult(JsonConvert.SerializeObject(json));
            context.HttpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
        }

        context.ExceptionHandled = true;
    }
}

public class JsonErrorResponse
{
    /// <summary>
    /// Error messages list
    /// </summary>
    public string[] Messages { get; set; }

    /// <summary>
    /// Debug information (inner exception)
    /// </summary>
    public string DeveloperMessage { get; set; }
}