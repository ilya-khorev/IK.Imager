using System.ComponentModel.DataAnnotations;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Configuration;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core
{
    public class ImageValidator: IImageValidator
    {
        private readonly IOptions<ImageLimitationSettings> _limitationSettings;

        private const string UnsupportedFormat = "Unsupported image format. Please use one of the following formats: {0}.";

        private const string IncorrectSize = "Image size must be between {0} and {1} bytes.";

        private const string IncorrectDimensions = "Image width must be between {0} and {1} px. Image height must be between {2} and {3} px.";

        public ImageValidator(IOptions<ImageLimitationSettings> limitationSettings)
        {
            _limitationSettings = limitationSettings;
        }

        public void CheckFormat(ImageFormat imageFormat)
        {
            var limits = _limitationSettings.Value;

            if (imageFormat == null || !limits.Types.Contains(imageFormat.ImageType.ToString()))
                throw new ValidationException(string.Format(UnsupportedFormat, string.Join(",", limits.Types)));
        }
        
        public void CheckSize(ImageSize imageSize)
        {
            var limits = _limitationSettings.Value;

            if (imageSize.Bytes > limits.SizeBytes.Max || imageSize.Bytes < limits.SizeBytes.Min)
                throw new ValidationException(string.Format(IncorrectSize, limits.SizeBytes.Min, limits.SizeBytes.Max));
            
            if (imageSize.Width > limits.Width.Max || imageSize.Width < limits.Width.Min || 
                imageSize.Height > limits.Height.Max || imageSize.Height < limits.Height.Min)
                throw new ValidationException(string.Format(IncorrectDimensions, limits.Width.Min, limits.Width.Max, limits.Height.Min, limits.Height.Max));

            //todo add aspect ratio restrictions
        }
    }
}