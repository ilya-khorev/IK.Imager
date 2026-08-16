using FluentValidation;
using IK.Imager.Api.Contract.Lookup;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Lookup;

public class LookupImagesRequestValidator : AbstractValidator<LookupImagesRequest>
{
    const int MaxImagesToLookup = 200;

    public LookupImagesRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .MaximumLength(ImageGroupConstraints.MaxImageGroupLength)
            .MinimumLength(ImageGroupConstraints.MinImageGroupLength);

        RuleFor(x => x.ImageIds)
            .NotEmpty()
            .Must(x => x.Length <= MaxImagesToLookup);
    }
}
