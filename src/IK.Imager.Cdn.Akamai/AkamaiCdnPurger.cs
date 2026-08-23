using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Cdn.Akamai;

/// <summary>
/// Purges by full url through the Akamai Fast Purge API.
/// </summary>
/// <remarks>
/// Deletes rather than invalidates. Akamai recommends invalidation in general, but an invalidated object
/// is revalidated against the origin, and by this point the blob is already gone.
/// </remarks>
public class AkamaiCdnPurger(HttpClient httpClient, ILogger<AkamaiCdnPurger> logger) : ICdnPurger
{
    private const string DeleteByUrlPath = "ccu/v3/delete/url/production";

    /// <summary>
    /// Fast Purge limits the body rather than the number of objects.
    /// </summary>
    private const int MaxBodyBytes = 50 * 1024;

    private const int EnvelopeBytes = 14;

    private const string Purged = "Akamai accepted a purge of {0} uri(s)";

    public async Task Purge(IReadOnlyCollection<Uri> contentUris, CancellationToken cancellationToken)
    {
        if (contentUris.Count == 0)
            return;

        foreach (var batch in Batch(contentUris))
            await PurgeBatch(batch, cancellationToken);

        logger.LogInformation(Purged, contentUris.Count);
    }

    private async Task PurgeBatch(IReadOnlyCollection<string> batch, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            DeleteByUrlPath, new PurgeRequest(batch), cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Akamai returned {(int)response.StatusCode} for a purge of {batch.Count} uri(s): " +
                await response.Content.ReadAsStringAsync(cancellationToken));
    }

    /// <summary>
    /// Splits the uris so that no request body passes <see cref="MaxBodyBytes"/>.
    /// </summary>
    /// <remarks>
    /// Sizes the entries rather than serializing each candidate batch. Image urls are generated names,
    /// so nothing in them is escaped on the way into json.
    /// </remarks>
    private static IEnumerable<List<string>> Batch(IReadOnlyCollection<Uri> contentUris)
    {
        var batch = new List<string>();
        var bodyBytes = EnvelopeBytes;

        foreach (var contentUri in contentUris)
        {
            var url = contentUri.AbsoluteUri;
            var urlBytes = Encoding.UTF8.GetByteCount(url) + 2;

            if (batch.Count > 0 && bodyBytes + urlBytes + 1 > MaxBodyBytes)
            {
                yield return batch;
                batch = [];
                bodyBytes = EnvelopeBytes;
            }

            //a comma once this is not the first entry
            bodyBytes += urlBytes + (batch.Count > 0 ? 1 : 0);
            batch.Add(url);
        }

        if (batch.Count > 0)
            yield return batch;
    }

    private sealed record PurgeRequest(
        [property: JsonPropertyName("objects")] IReadOnlyCollection<string> Objects);
}
