using FluentValidation;
using IK.Imager.Api.Contract.ImageLookup;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.ImageLookup;

public class ImageLookupByIdRequestValidator : AbstractValidator<ImageLookupByIdRequest>
{
    const int MaxImagesToLookup = 200;

    public ImageLookupByIdRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .MaximumLength(ValidationConstants.MaxImageGroupLength)
            .MinimumLength(ValidationConstants.MinImageGroupLength);

        RuleFor(x => x.ImageIds)
            .NotEmpty()
            .Must(x => x.Length <= MaxImagesToLookup);
    }
}
