using System;
using System.Linq;
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

    /// <summary>
    /// How many thumbnail widths one upload may ask for. The configured widths carry no such bound because
    /// an operator sets them once, whereas this is per request - and every width is one more resize of the
    /// original before the image gets its thumbnails.
    /// </summary>
    internal const int MaxThumbnailTargetWidths = 10;

    //an empty list is rejected rather than read as "no thumbnails at all" - the caller who wants the
    //configured widths omits the property, and guessing between the two would be a silent surprise
    internal static readonly string InvalidThumbnailTargetWidthCount =
        $"ThumbnailTargetWidths must hold between 1 and {MaxThumbnailTargetWidths} widths. " +
        "Omit it to use the widths configured for the service.";

    internal const string InvalidThumbnailTargetWidth =
        "Every thumbnail target width must be greater than zero.";

    internal const string DuplicateThumbnailTargetWidths =
        "ThumbnailTargetWidths must not repeat a width - one width produces one thumbnail.";

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

        RuleFor(x => x.ThumbnailTargetWidths)
            .Must(widths => widths!.Length is > 0 and <= MaxThumbnailTargetWidths)
            .WithMessage(InvalidThumbnailTargetWidthCount)
            .Must(widths => Array.TrueForAll(widths!, width => width > 0))
            .WithMessage(InvalidThumbnailTargetWidth)
            .Must(widths => HasDistinctWidths(widths!))
            .WithMessage(DuplicateThumbnailTargetWidths)
            .When(x => x.ThumbnailTargetWidths != null);
    }

    internal static bool HasDistinctWidths(int[] widths) =>
        widths.Distinct().Count() == widths.Length;
}
