using System.Collections.Generic;
using System.Linq;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Validation;
using IK.Imager.Core.Settings;
using IK.Imager.Utils;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core.Validation
{
    public class ImageValidator: IImageValidator
    {
        private readonly IOptions<ImageLimitationSettings> _limitationSettings;

        private const string UnsupportedFormat = "Unsupported image format. Please use one of the following formats: {0}.";
        private const string IncorrectSize = "Image size must be between {0} and {1} bytes.";
        private const string IncorrectDimension = "Image width must be between {0} and {1} px. Image height must be between {2} and {3} px.";
        private const string IncorrectAspectRatio = "Image aspect ratio must be between {0} and {1}.";

        public const string UnsupportedFormatKey = "image/unsupported-format";
        public const string IncorrectSizeKey = "image/incorrect-size";
        public const string IncorrectDimensionKey = "image/incorrect-dimension";
        public const string IncorrectAspectRatioKey = "image/incorrect-aspect-ratio";
        
        public ImageValidator(IOptionsSnapshot<ImageLimitationSettings> limitationSettings)
        {
            _limitationSettings = limitationSettings;
        }

        public ValidationResult CheckFormat(ImageFormat imageFormat)
        {
            ArgumentHelper.AssertNotNull(nameof(imageFormat), imageFormat);

            var limits = _limitationSettings.Value;

            if (imageFormat == null || !limits.Types.Contains(imageFormat.ImageType.ToString()))
            {
                ValidationError validationError = new ValidationError(UnsupportedFormatKey, string.Format(UnsupportedFormat, string.Join(",", limits.Types)));
                return new ValidationResult(validationError);
            }

            return ValidationResult.Success;
        }
        
        //todo move errors to resources
       
        public ValidationResult CheckSize(ImageSize imageSize)
        {
            ArgumentHelper.AssertNotNull(nameof(imageSize), imageSize);
            
            var limits = _limitationSettings.Value;

            List<ValidationError> validationErrors = new List<ValidationError>();
            if (imageSize.Bytes > limits.SizeBytes.Max || imageSize.Bytes < limits.SizeBytes.Min)
                validationErrors.Add(new ValidationError(IncorrectSizeKey, string.Format(IncorrectSize, limits.SizeBytes.Min, limits.SizeBytes.Max)));

            if (imageSize.Width > limits.Width.Max || imageSize.Width < limits.Width.Min || 
                imageSize.Height > limits.Height.Max || imageSize.Height < limits.Height.Min)
                validationErrors.Add(new ValidationError(IncorrectDimensionKey, string.Format(IncorrectDimension, limits.Width.Min, limits.Width.Max, limits.Height.Min, limits.Height.Max)));

            if (imageSize.AspectRatio > limits.AspectRatio.Max || imageSize.AspectRatio < limits.AspectRatio.Min)
                validationErrors.Add(new ValidationError(IncorrectAspectRatioKey, string.Format(IncorrectAspectRatio, limits.AspectRatio.Min, limits.AspectRatio.Max)));

            if (validationErrors.Any())
                return new ValidationResult(validationErrors);

            return ValidationResult.Success;
        }
    }
}