namespace IK.Imager.Cdn.Cloudflare;

public class CloudflareCdnSettings
{
    /// <summary>
    /// Id of the zone the images are served from.
    /// </summary>
    public string ZoneId { get; set; } = null!;

    /// <summary>
    /// API token with the Zone - Cache Purge - Purge permission.
    /// </summary>
    public string ApiToken { get; set; } = null!;
}
