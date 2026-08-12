using FluentValidation;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.ImageUpload;

public class UploadImageFileRequestValidator : AbstractValidator<UploadImageFileRequest>
{
    public UploadImageFileRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .NotEmpty()
            .MaximumLength(ValidationConstants.MaxImageGroupLength)
            .MinimumLength(ValidationConstants.MinImageGroupLength);
    }
}
