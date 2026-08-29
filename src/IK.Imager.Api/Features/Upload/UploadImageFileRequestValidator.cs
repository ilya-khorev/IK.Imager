using FluentValidation;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Upload;

public class UploadImageFileRequestValidator : AbstractValidator<UploadImageFileRequest>
{
    internal const string InvalidImageId =
        "ImageId must be lowercase letters, digits, and dots, underscores or hyphens between them.";

    internal const string InvalidCollection =
        "Collection must be lowercase letters, digits, and dots, underscores or hyphens between them.";

    internal const string CollectionRequiredForPath =
        "IncludeCollectionInPath needs a Collection to put in the path.";

    public UploadImageFileRequestValidator()
    {
        RuleFor(x => x.ImageId)
            .MaximumLength(IdentifierConstraints.MaxImageIdLength)
            .Must(IdentifierConstraints.IsWellFormed)
            .WithMessage(InvalidImageId)
            .When(x => x.ImageId != null);

        RuleFor(x => x.Collection)
            .MinimumLength(IdentifierConstraints.MinCollectionLength)
            .MaximumLength(IdentifierConstraints.MaxCollectionLength)
            .Must(IdentifierConstraints.IsWellFormed)
            .WithMessage(InvalidCollection)
            .When(x => x.Collection != null);

        RuleFor(x => x.IncludeCollectionInPath)
            .Must((request, _) => !string.IsNullOrEmpty(request.Collection))
            .WithMessage(CollectionRequiredForPath)
            .When(x => x.IncludeCollectionInPath);
    }
}
