using System;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Cdn;
using IK.Imager.Core.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IK.Imager.Core.Tests
{
    public class CdnServiceTests
    {
        private static readonly Uri TestImageUri = new("https://ikimagesstorageaccount.blob.core.windows.net/images/d41be6cb6880421aa87fa401f79ed0f6fb1277.jpg");

        [Fact]
        public void TryTransformToCdnUri_CdnOnInSettings_CorrectlyReplacedImageHosToCdnHost()
        {
            var optionsMock = new Mock<IOptions<CdnSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new CdnSettings
            {
                Uri = new Uri("https://ikimager.azureedge.net")
            });

            ICdnService cdnService = new CdnService(optionsMock.Object);
            var transformedUri = cdnService.TryTransformToCdnUri(TestImageUri);
            Assert.Equal(new Uri("https://ikimager.azureedge.net/images/d41be6cb6880421aa87fa401f79ed0f6fb1277.jpg"),
                transformedUri);
        }

        [Fact]
        public void TryTransformToCdnUri_CdnOffInSettings_ReturnsOriginalImage()
        {
            var optionsMock = new Mock<IOptions<CdnSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new CdnSettings
            {
                Uri = null
            });

            ICdnService cdnService = new CdnService(optionsMock.Object);
            var transformedUri = cdnService.TryTransformToCdnUri(TestImageUri);
            Assert.Equal(TestImageUri, transformedUri);
        }
    }
}