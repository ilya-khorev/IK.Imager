using System;

namespace IK.Imager.Core.Settings
{
    public class CdnSettings
    {
        /// <summary>
        /// Null when no CDN is configured - <see cref="Cdn.CdnService"/> then leaves blob urls untouched.
        /// </summary>
        public Uri? Uri { get; set; }
    }
}