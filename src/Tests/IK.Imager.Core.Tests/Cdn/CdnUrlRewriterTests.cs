using System;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Cdn;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IK.Imager.Core.Tests.Cdn
{
    public class CdnUrlRewriterTests
    {
        private static readonly Uri TestImageUri = new("https://ikimagesstorageaccount.blob.core.windows.net/images/d41be6cb6880421aa87fa401f79ed0f6fb1277.jpg");

        [Fact]
        public void Rewrite_CdnConfigured_ReplacesImageHostWithCdnHost()
        {
            var optionsMock = new Mock<IOptions<CdnSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new CdnSettings
            {
                Uri = new Uri("https://ikimager.azureedge.net")
            });

            ICdnUrlRewriter cdnService = new CdnUrlRewriter(optionsMock.Object);
            var transformedUri = cdnService.Rewrite(TestImageUri);
            Assert.Equal(new Uri("https://ikimager.azureedge.net/images/d41be6cb6880421aa87fa401f79ed0f6fb1277.jpg"),
                transformedUri);
        }

        [Fact]
        public void Rewrite_CdnNotConfigured_ReturnsOriginalUri()
        {
            var optionsMock = new Mock<IOptions<CdnSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new CdnSettings
            {
                Uri = null
            });

            ICdnUrlRewriter cdnService = new CdnUrlRewriter(optionsMock.Object);
            var transformedUri = cdnService.Rewrite(TestImageUri);
            Assert.Equal(TestImageUri, transformedUri);
        }
    }
}
