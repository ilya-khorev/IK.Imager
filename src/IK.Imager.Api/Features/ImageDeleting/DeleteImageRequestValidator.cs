using FluentValidation;
using IK.Imager.Api.Contract.ImageDeleting;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.ImageDeleting;

public class DeleteImageRequestValidator: AbstractValidator<DeleteImageRequest>
{
    public DeleteImageRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .MaximumLength(ValidationConstants.MaxImageGroupLength)
            .MinimumLength(ValidationConstants.MinImageGroupLength);

        RuleFor(x => x.ImageId)
            .NotEmpty();
    }
}
