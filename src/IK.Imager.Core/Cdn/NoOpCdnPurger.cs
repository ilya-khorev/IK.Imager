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
    private const string NoPurger = "No CDN purger is registered - not purging {0} uri(s): {1}";

    public Task Purge(IReadOnlyCollection<Uri> contentUris, CancellationToken cancellationToken)
    {
        if (contentUris.Count > 0)
            logger.LogDebug(NoPurger, contentUris.Count, string.Join(", ", contentUris));

        return Task.CompletedTask;
    }
}
