using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Core.Cdn;

/// <summary>
/// Used until a provider module registers a real purger. A deployment without a CDN has no edge cache
/// to purge, which is also the case for local runs and for the test suites.
/// </summary>
public class NoOpCdnPurger(ILogger<NoOpCdnPurger> logger) : ICdnPurger
{
    public Task Purge(IReadOnlyCollection<Uri> contentUris, CancellationToken cancellationToken)
    {
        if (contentUris.Count > 0)
            logger.NotPurging(contentUris.Count);

        return Task.CompletedTask;
    }
}
