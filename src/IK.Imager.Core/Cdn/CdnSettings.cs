using System;

namespace IK.Imager.Core.Cdn;

public class CdnSettings
{
    /// <summary>
    /// Null when no CDN is configured - <see cref="ImageUrlBuilder"/> then hands out blob urls as they are.
    /// </summary>
    public Uri? Uri { get; set; }
}
