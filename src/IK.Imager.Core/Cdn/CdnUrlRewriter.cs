using System;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace IK.Imager.Core.Cdn;

public class CdnUrlRewriter(IOptions<CdnSettings> cdnSettings) : ICdnUrlRewriter
{
    /// <inheritdoc />
    public Uri Rewrite(Uri originalUri)
    {
        if (cdnSettings.Value.Uri == null)
            return originalUri;

        var builder = new UriBuilder(originalUri)
        {
            Host = cdnSettings.Value.Uri.Host,
            Scheme = cdnSettings.Value.Uri.Scheme
        };

        return builder.Uri;
    }
}
