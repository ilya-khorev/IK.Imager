using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IK.Imager.Cdn.Cloudflare;

/// <summary>
/// Purges by full url through the Cloudflare cache API.
/// </summary>
public class CloudflareCdnPurger(
    HttpClient httpClient,
    IOptions<CloudflareCdnSettings> settings,
    ILogger<CloudflareCdnPurger> logger) : ICdnPurger
{
    /// <summary>
    /// Cloudflare rejects a purge-by-url call carrying more than this. Enterprise zones are allowed 500,
    /// which is not worth a setting until someone has one.
    /// </summary>
    private const int MaxUrisPerRequest = 100;

    public async Task Purge(IReadOnlyCollection<Uri> contentUris, CancellationToken cancellationToken)
    {
        if (contentUris.Count == 0)
            return;

        foreach (var batch in contentUris.Chunk(MaxUrisPerRequest))
            await PurgeBatch(batch, cancellationToken);

        logger.Purged(contentUris.Count, settings.Value.ZoneId);
    }

    private async Task PurgeBatch(IReadOnlyCollection<Uri> batch, CancellationToken cancellationToken)
    {
        var request = new PurgeRequest(batch.Select(x => x.AbsoluteUri).ToArray());

        var response = await httpClient.PostAsJsonAsync(
            $"client/v4/zones/{settings.Value.ZoneId}/purge_cache", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.PurgeFailed((int)response.StatusCode, batch.Count, settings.Value.ZoneId);
            throw new HttpRequestException(
                $"Cloudflare returned {(int)response.StatusCode} for a purge of {batch.Count} uri(s): " +
                await response.Content.ReadAsStringAsync(cancellationToken));
        }

        //Cloudflare does not promise that the status code and the envelope agree, so a 200 carrying
        //success:false has to be caught here or the purge silently does nothing
        var result = await response.Content.ReadFromJsonAsync<PurgeResponse>(cancellationToken);
        if (result is { Success: false })
        {
            var purgeErrors = Describe(result.Errors);
            logger.PurgeRejected(batch.Count, settings.Value.ZoneId, purgeErrors);
            throw new HttpRequestException($"Cloudflare rejected a purge of {batch.Count} uri(s): {purgeErrors}");
        }
    }

    private static string Describe(IReadOnlyCollection<PurgeError>? errors) =>
        errors == null || errors.Count == 0
            ? "no error details were returned"
            : string.Join("; ", errors.Select(x => $"{x.Code} {x.Message}"));

    private sealed record PurgeRequest(
        [property: JsonPropertyName("files")] IReadOnlyCollection<string> Files);

    private sealed record PurgeResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("errors")] IReadOnlyCollection<PurgeError>? Errors);

    private sealed record PurgeError(
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("message")] string? Message);
}
