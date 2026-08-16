using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IK.Imager.Core.Abstractions.Upload;

/// <summary>
/// The outcome of checking an image itself - its format, size, dimensions and aspect ratio - against the
/// configured limits. Prefixed to keep it apart from FluentValidation's and DataAnnotations'
/// <c>ValidationResult</c>, both of which are in play in this solution.
/// </summary>
public record ImageValidationResult
{
    public ImageValidationResult(bool isValid, IList<ImageValidationError> validationErrors)
    {
        if (isValid && validationErrors?.Count > 0)
            throw new ArgumentException("ValidationErrors must be empty for valid result");

        ValidationErrors = new ReadOnlyCollection<ImageValidationError>(validationErrors ?? new List<ImageValidationError>());

        IsValid = isValid;
    }

    public ImageValidationResult(IList<ImageValidationError> validationErrors) : this(false, validationErrors)
    {
    }

    public ImageValidationResult(ImageValidationError validationError) : this(new List<ImageValidationError> { validationError })
    {
    }

    public static ImageValidationResult Success => new(true, new List<ImageValidationError>());

    public bool IsValid { get; init; }

    public ReadOnlyCollection<ImageValidationError> ValidationErrors { get; init; }
}

public record ImageValidationError(string Key, string ErrorMessage);
