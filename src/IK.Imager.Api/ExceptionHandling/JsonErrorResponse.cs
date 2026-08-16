#pragma warning disable 1591
namespace IK.Imager.Api.ExceptionHandling;

/// <summary>
/// The body every 500 carries. Public and in a file of its own because it is part of what the service
/// returns, not an implementation detail of <see cref="GlobalExceptionHandler"/>.
/// </summary>
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
