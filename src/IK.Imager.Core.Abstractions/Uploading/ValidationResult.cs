using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IK.Imager.Core.Abstractions.Uploading;

public record ValidationResult
{
    public ValidationResult(bool isValid, IList<ValidationError> validationErrors)
    {
        if (isValid && validationErrors?.Count > 0)
            throw new ArgumentException("ValidationErrors must be empty for valid result");

        ValidationErrors = new ReadOnlyCollection<ValidationError>(validationErrors ?? new List<ValidationError>());

        IsValid = isValid;
    }

    public ValidationResult(IList<ValidationError> validationErrors) : this(false, validationErrors)
    {
    }

    public ValidationResult(ValidationError validationError) : this(new List<ValidationError> { validationError })
    {
    }

    public static ValidationResult Success => new(true, new List<ValidationError>());

    public bool IsValid { get; init; }

    public ReadOnlyCollection<ValidationError> ValidationErrors { get; init; }
}

public record ValidationError(string Key, string ErrorMessage);
