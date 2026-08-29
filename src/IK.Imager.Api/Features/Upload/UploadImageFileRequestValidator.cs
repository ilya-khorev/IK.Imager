using FluentValidation;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Upload;

public class UploadImageFileRequestValidator : AbstractValidator<UploadImageFileRequest>
{
    public UploadImageFileRequestValidator()
    {
        RuleFor(x => x.Collection)
            .MinimumLength(IdentifierConstraints.MinCollectionLength)
            .MaximumLength(IdentifierConstraints.MaxCollectionLength)
            .Must(IdentifierConstraints.IsWellFormed)
            .WithMessage(InvalidCollection)
            .When(x => x.Collection != null);
    }

    internal const string InvalidCollection =
        "Collection must be lowercase letters, digits, and dots, underscores or hyphens between them.";
}
