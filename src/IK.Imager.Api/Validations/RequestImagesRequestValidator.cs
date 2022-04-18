using FluentValidation;
using IK.Imager.Api.Contract;
#pragma warning disable 1591

namespace IK.Imager.Api.Validations;

public class RequestImagesRequestValidator: AbstractValidator<SearchImagesByIdRequest>
{
    const int MaxImagesToRequest = 200;
        
    public RequestImagesRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .MaximumLength(ValidationConstants.MaxImageGroupLength)
            .MinimumLength(ValidationConstants.MinImageGroupLength);

        RuleFor(x => x.ImageIds)
            .NotEmpty()
            .Must(x => x.Length <= MaxImagesToRequest);
    }
}