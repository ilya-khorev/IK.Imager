using IK.Imager.Core.Settings;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core.Tests.Mocks
{
    public class MockImageThumbnailsSettings: IOptions<ImageThumbnailsSettings>
    {
        public ImageThumbnailsSettings Value { get; } = new ImageThumbnailsSettings
        {
            TargetWidth = new [] { 200, 300, 500 }
        };
    }
}