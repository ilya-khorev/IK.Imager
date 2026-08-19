using FluentValidation;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Upload;

public class UploadImageFileRequestValidator : AbstractValidator<UploadImageFileRequest>
{
    public UploadImageFileRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .NotEmpty()
            .MaximumLength(ImageGroupConstraints.MaxImageGroupLength)
            .MinimumLength(ImageGroupConstraints.MinImageGroupLength);
    }
}
