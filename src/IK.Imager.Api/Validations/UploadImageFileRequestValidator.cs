using FluentValidation;
using IK.Imager.Api.Contract;
#pragma warning disable 1591

namespace IK.Imager.Api.Validations;

public class UploadImageFileRequestValidator: AbstractValidator<UploadImageFileRequest>
{
    public UploadImageFileRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .NotEmpty()
            .MaximumLength(ValidationConstants.MaxImageGroupLength)
            .MinimumLength(ValidationConstants.MinImageGroupLength);
    }
}