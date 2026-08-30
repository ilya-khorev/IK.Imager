using System;
using FluentValidation;
using IK.Imager.Api.Contract.Upload;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Upload;

public class UploadImageByUrlRequestValidator : AbstractValidator<UploadImageByUrlRequest>
{
    private const string IncorrectUrlFormat = "Image Url is not well formed. Please specify an absolute url path.";
    private const string InvalidImageId = UploadImageFileRequestValidator.InvalidImageId;
    private const string InvalidCollection = UploadImageFileRequestValidator.InvalidCollection;
    private const string CollectionRequiredForPath = UploadImageFileRequestValidator.CollectionRequiredForPath;

    public UploadImageByUrlRequestValidator()
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

        RuleFor(x => x.ImageUrl)
            .NotEmpty()
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
            .WithMessage(IncorrectUrlFormat);
    }
}
