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
    private const int MaxThumbnailTargetWidths = UploadImageFileRequestValidator.MaxThumbnailTargetWidths;
    private static readonly string InvalidThumbnailTargetWidthCount = UploadImageFileRequestValidator.InvalidThumbnailTargetWidthCount;
    private const string InvalidThumbnailTargetWidth = UploadImageFileRequestValidator.InvalidThumbnailTargetWidth;
    private const string DuplicateThumbnailTargetWidths = UploadImageFileRequestValidator.DuplicateThumbnailTargetWidths;

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

        RuleFor(x => x.ThumbnailTargetWidths)
            .Must(widths => widths!.Length is > 0 and <= MaxThumbnailTargetWidths)
            .WithMessage(InvalidThumbnailTargetWidthCount)
            .Must(widths => Array.TrueForAll(widths!, width => width > 0))
            .WithMessage(InvalidThumbnailTargetWidth)
            .Must(widths => UploadImageFileRequestValidator.HasDistinctWidths(widths!))
            .WithMessage(DuplicateThumbnailTargetWidths)
            .When(x => x.ThumbnailTargetWidths != null);
    }
}
