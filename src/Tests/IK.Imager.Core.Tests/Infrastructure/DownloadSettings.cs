using IK.Imager.Core.Upload;
using Microsoft.Extensions.Options;
using Moq;

namespace IK.Imager.Core.Tests.Infrastructure;

public static class DownloadSettings
{
    /// <summary>
    /// The download options ImageDownloader reads. The defaults are the production ones, so a test only
    /// states the redirect budget when that is what it is about.
    /// </summary>
    public static IOptionsMonitor<ImageDownloadSettings> WithMaxRedirects(int maxRedirects = 5)
    {
        var monitorMock = new Mock<IOptionsMonitor<ImageDownloadSettings>>();
        monitorMock.Setup(x => x.CurrentValue).Returns(new ImageDownloadSettings { MaxRedirects = maxRedirects });

        return monitorMock.Object;
    }
}
