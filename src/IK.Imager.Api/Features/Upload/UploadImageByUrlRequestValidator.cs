using System;
using FluentValidation;
using IK.Imager.Api.Contract.Upload;
using IK.Imager.Api.Validation;
#pragma warning disable 1591

namespace IK.Imager.Api.Features.Upload;

public class UploadImageByUrlRequestValidator : AbstractValidator<UploadImageByUrlRequest>
{
    private const string IncorrectUrlFormat = "Image Url is not well formed. Please specify an absolute url path.";

    public UploadImageByUrlRequestValidator()
    {
        RuleFor(x => x.ImageGroup)
            .NotEmpty()
            .MaximumLength(ImageGroupConstraints.MaxImageGroupLength)
            .MinimumLength(ImageGroupConstraints.MinImageGroupLength);

        RuleFor(x => x.ImageUrl)
            .NotEmpty()
            .Must(x => Uri.IsWellFormedUriString(x, UriKind.Absolute))
            .WithMessage(IncorrectUrlFormat);
    }
}
