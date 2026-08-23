using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Cdn.Fastly;

/// <summary>
/// Purges by full url through the Fastly purge API.
/// </summary>
/// <remarks>
/// One request per uri. Fastly has no bulk purge by url - its bulk endpoint takes surrogate keys only -
/// and a delete purges an original plus its thumbnails, which is a handful of uris against a limit of
/// 100,000 purges per hour.
/// </remarks>
public class FastlyCdnPurger(HttpClient httpClient, ILogger<FastlyCdnPurger> logger) : ICdnPurger
{
    private const string Purged = "Fastly accepted a purge of {0} uri(s)";

    public async Task Purge(IReadOnlyCollection<Uri> contentUris, CancellationToken cancellationToken)
    {
        if (contentUris.Count == 0)
            return;

        foreach (var contentUri in contentUris)
            await PurgeOne(contentUri, cancellationToken);

        logger.LogInformation(Purged, contentUris.Count);
    }

    private async Task PurgeOne(Uri contentUri, CancellationToken cancellationToken)
    {
        //a hard purge - the blob is already gone, so marking the object stale with Fastly-Soft-Purge
        //would let stale-while-revalidate keep serving a deleted image
        var response = await httpClient.PostAsync(ToPurgeTarget(contentUri), null, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Fastly returned {(int)response.StatusCode} for a purge of {contentUri}: " +
                await response.Content.ReadAsStringAsync(cancellationToken));
    }

    //Fastly addresses a cached object by host and path with the scheme stripped
    private static string ToPurgeTarget(Uri contentUri) =>
        $"purge/{contentUri.Authority}{contentUri.AbsolutePath}";
}
