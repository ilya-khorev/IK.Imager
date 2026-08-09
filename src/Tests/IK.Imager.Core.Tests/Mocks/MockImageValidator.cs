using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Uploading;

namespace IK.Imager.Core.Tests.Mocks
{
    public class MockImageValidator: IImageValidator
    {
        public ValidationResult CheckFormat(ImageFormat? imageFormat)
        {
            return ValidationResult.Success;
        }

        public ValidationResult CheckSize(ImageSize imageSize)
        {
            return ValidationResult.Success;
        }
    }
}