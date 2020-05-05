using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Models;

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