using FluentValidation;
using IK.Imager.Api.Contract.Delete;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Delete;

public class DeleteImageRequestValidator : AbstractValidator<DeleteImageRequest>
{
    public DeleteImageRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .MaximumLength(ImageGroupConstraints.MaxImageGroupLength)
            .MinimumLength(ImageGroupConstraints.MinImageGroupLength);

        RuleFor(x => x.ImageId)
            .NotEmpty();
    }
}
