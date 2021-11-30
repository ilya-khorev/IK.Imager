using System;
using FluentValidation;
using IK.Imager.Api.Contract;
#pragma warning disable 1591

namespace IK.Imager.Api.Validations
{
    public class UploadImageRequestValidator: AbstractValidator<UploadImageRequest>
    {
        private const string IncorrectUrlFormat = "Image Url is not well formed. Please specify an absolute url path.";
        
        public UploadImageRequestValidator()
        {
            RuleFor(x => x.ImageGroup)
                .NotEmpty()
                .MaximumLength(ValidationConstants.MaxImageGroupLength)
                .MinimumLength(ValidationConstants.MinImageGroupLength);

            RuleFor(x => x.ImageUrl)
                .NotEmpty()
                .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
                .WithMessage(IncorrectUrlFormat);
        }
    }
}