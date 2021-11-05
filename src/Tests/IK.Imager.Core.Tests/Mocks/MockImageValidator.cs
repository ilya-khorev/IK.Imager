using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Validation;

namespace IK.Imager.Core.Tests.Mocks
{
    public class MockImageValidator: IImageValidator
    {
        public void CheckFormat(ImageFormat imageFormat)
        {
        }

        public void CheckSize(ImageSize imageSize)
        {
        }
    }
}