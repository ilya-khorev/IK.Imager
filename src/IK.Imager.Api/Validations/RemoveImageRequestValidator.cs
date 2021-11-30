using FluentValidation;
using IK.Imager.Api.Contract;
#pragma warning disable 1591

namespace IK.Imager.Api.Validations
{
    public class RemoveImageRequestValidator: AbstractValidator<DeleteImageRequest>
    {
        public RemoveImageRequestValidator()
        {
            RuleFor(x => x.ImageGroup)
                .MaximumLength(ValidationConstants.MaxImageGroupLength)
                .MinimumLength(ValidationConstants.MinImageGroupLength);

            RuleFor(x => x.ImageId)
                .NotEmpty();
        }
    }
}