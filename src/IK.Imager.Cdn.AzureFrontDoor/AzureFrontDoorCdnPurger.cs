using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Cdn;
using Azure.ResourceManager.Cdn.Models;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IK.Imager.Cdn.AzureFrontDoor;

/// <summary>
/// Purges by path through the Azure Front Door management API.
/// </summary>
public class AzureFrontDoorCdnPurger(
    ArmClient armClient,
    IOptions<AzureFrontDoorCdnSettings> settings,
    ILogger<AzureFrontDoorCdnPurger> logger) : ICdnPurger
{
    /// <summary>
    /// Front Door counts the combination of domain and path, and rejects a purge submitted before the
    /// previous one has finished - which takes about ten minutes. Splitting a larger batch would therefore
    /// fail on its second request, so this is a limit rather than a chunk size.
    /// </summary>
    private const int MaxUrisPerRequest = 100;

    public async Task Purge(IReadOnlyCollection<Uri> contentUris, CancellationToken cancellationToken)
    {
        if (contentUris.Count == 0)
            return;

        if (contentUris.Count > MaxUrisPerRequest)
        {
            logger.BatchTooLarge(contentUris.Count, settings.Value.EndpointName, MaxUrisPerRequest);
            throw new ArgumentException(
                $"Front Door purges at most {MaxUrisPerRequest} uris at a time and refuses a second purge " +
                $"until the first one completes, so {contentUris.Count} uris cannot be purged as a batch.",
                nameof(contentUris));
        }

        var content = new FrontDoorPurgeContent(contentUris.Select(x => x.AbsolutePath).ToArray());

        //what Front Door purges when the domains are left out is not documented, and the uris are already
        //on the CDN host, so the hosts they carry are the answer
        foreach (var host in contentUris.Select(x => x.Host).Distinct(StringComparer.OrdinalIgnoreCase))
            content.Domains.Add(host);

        //Started rather than Completed - the call returns once Azure accepts the purge, whereas propagation
        //takes minutes and waiting for it would hold the Service Bus message lock
        await GetEndpoint().PurgeContentAsync(WaitUntil.Started, content, cancellationToken);

        logger.Purged(contentUris.Count, settings.Value.EndpointName);
    }

    private FrontDoorEndpointResource GetEndpoint()
    {
        var endpointSettings = settings.Value;

        var id = FrontDoorEndpointResource.CreateResourceIdentifier(endpointSettings.SubscriptionId,
            endpointSettings.ResourceGroupName, endpointSettings.ProfileName, endpointSettings.EndpointName);

        return armClient.GetFrontDoorEndpointResource(id);
    }
}
