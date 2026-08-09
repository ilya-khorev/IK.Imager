using FluentValidation;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.ImageLookup;

public class SearchImagesByIdRequestValidator: AbstractValidator<SearchImagesByIdRequest>
{
    const int MaxImagesToRequest = 200;

    public SearchImagesByIdRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .MaximumLength(ValidationConstants.MaxImageGroupLength)
            .MinimumLength(ValidationConstants.MinImageGroupLength);

        RuleFor(x => x.ImageIds)
            .NotEmpty()
            .Must(x => x.Length <= MaxImagesToRequest);
    }
}
