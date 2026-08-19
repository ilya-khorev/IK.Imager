using System.IO;

namespace IK.Imager.Api.Tests.Infrastructure;

/// <summary>
/// The sample files these tests upload, linked in from IK.Imager.Core.Tests by the .csproj.
///
/// The widths matter: Thumbnails:TargetWidth is [200, 400, 1000] in appsettings.json, and
/// ThumbnailGenerator only produces a thumbnail for a target strictly narrower than the original. So the
/// three images below are chosen to land on three, two and no thumbnails respectively.
/// </summary>
internal static class TestImages
{
    private const string ImagesDirectory = "Images";

    /// <summary>1200 px wide - wider than all three targets, so 200, 400 and 1000 are all generated.</summary>
    public const string Jpeg1200X900 = "1043-1200x900.jpg";

    /// <summary>800 px wide - the 1000 target is skipped, leaving 200 and 400.</summary>
    public const string Jpeg800X600 = "1043-800x600.jpg";

    /// <summary>1000 px wide - only 200 and 400 are narrower than the original, 1000 is not.</summary>
    public const string Png1000X1000 = "1060-1000x1000.png";

    /// <summary>200 px wide - no target is narrower, so generation stops before it resizes anything.</summary>
    public const string Gif200X200 = "giphy_200x200.gif";

    /// <summary>Not an image at all - ImageValidator rejects it and the upload comes back a 400.</summary>
    public const string NotAnImage = "not-an-image.txt";

    public static string PathOf(string fileName) => Path.Combine(ImagesDirectory, fileName);
}
