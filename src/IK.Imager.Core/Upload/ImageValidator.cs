using System;
using System.Collections.Generic;
using System.Linq;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Upload;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core.Upload;

public class ImageValidator(IOptionsSnapshot<ImageLimitationsSettings> limitationSettings) : IImageValidator
{

    private const string UnsupportedFormat = "Unsupported image format. Please use one of the following formats: {0}.";
    private const string IncorrectSize = "Image size must be between {0} and {1} bytes.";
    private const string IncorrectDimension = "Image width must be between {0} and {1} px. Image height must be between {2} and {3} px.";
    private const string IncorrectAspectRatio = "Image aspect ratio must be between {0} and {1}.";

    public const string UnsupportedFormatKey = "image/unsupported-format";
    public const string IncorrectSizeKey = "image/incorrect-size";
    public const string IncorrectDimensionKey = "image/incorrect-dimension";
    public const string IncorrectAspectRatioKey = "image/incorrect-aspect-ratio";

    public ImageValidationResult CheckFormat(ImageFormat? imageFormat)
    {
        var limits = limitationSettings.Value;

        if (imageFormat == null || !limits.Types.Contains(imageFormat.ImageType.ToString()))
        {
            ImageValidationError validationError = new ImageValidationError(UnsupportedFormatKey, string.Format(UnsupportedFormat, string.Join(",", limits.Types)));
            return new ImageValidationResult(validationError);
        }

        return ImageValidationResult.Success;
    }

    //todo move errors to resources

    public ImageValidationResult CheckSize(ImageSize imageSize)
    {
        ArgumentNullException.ThrowIfNull(imageSize);

        var limits = limitationSettings.Value;

        List<ImageValidationError> validationErrors = new List<ImageValidationError>();
        if (imageSize.Bytes > limits.SizeBytes.Max || imageSize.Bytes < limits.SizeBytes.Min)
            validationErrors.Add(new ImageValidationError(IncorrectSizeKey, string.Format(IncorrectSize, limits.SizeBytes.Min, limits.SizeBytes.Max)));

        if (imageSize.Width > limits.Width.Max || imageSize.Width < limits.Width.Min ||
            imageSize.Height > limits.Height.Max || imageSize.Height < limits.Height.Min)
            validationErrors.Add(new ImageValidationError(IncorrectDimensionKey, string.Format(IncorrectDimension, limits.Width.Min, limits.Width.Max, limits.Height.Min, limits.Height.Max)));

        if (imageSize.AspectRatio > limits.AspectRatio.Max || imageSize.AspectRatio < limits.AspectRatio.Min)
            validationErrors.Add(new ImageValidationError(IncorrectAspectRatioKey, string.Format(IncorrectAspectRatio, limits.AspectRatio.Min, limits.AspectRatio.Max)));

        if (validationErrors.Any())
            return new ImageValidationResult(validationErrors);

        return ImageValidationResult.Success;
    }
}
