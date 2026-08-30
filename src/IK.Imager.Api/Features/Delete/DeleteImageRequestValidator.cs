using FluentValidation;
using IK.Imager.Api.Contract.Delete;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Delete;

public class DeleteImageRequestValidator : AbstractValidator<DeleteImageRequest>
{
    public DeleteImageRequestValidator()
    {
        RuleFor(x => x.ImageId)
            .NotEmpty();
    }
}
