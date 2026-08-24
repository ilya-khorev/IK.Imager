using IK.Imager.Core.Upload;
using Microsoft.Extensions.Options;
using Moq;

namespace IK.Imager.Core.Tests.Infrastructure;

public static class ImageLimitations
{
    /// <summary>
    /// The options ImageDownloader reads. Only the size range matters to it, so the other thresholds
    /// are left unset.
    /// </summary>
    public static IOptionsMonitor<ImageLimitationsSettings> WithMaxSizeBytes(int maxSizeBytes)
    {
        var monitorMock = new Mock<IOptionsMonitor<ImageLimitationsSettings>>();
        monitorMock.Setup(x => x.CurrentValue).Returns(new ImageLimitationsSettings
        {
            SizeBytes = new ValueRange<int> { Min = 1, Max = maxSizeBytes }
        });

        return monitorMock.Object;
    }
}
