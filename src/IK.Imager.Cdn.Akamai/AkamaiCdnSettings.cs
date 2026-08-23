namespace IK.Imager.Cdn.Akamai;

public class AkamaiCdnSettings
{
    /// <summary>
    /// Per account API host, as issued with the credentials.
    /// </summary>
    public string Host { get; set; } = null!;

    /// <summary>
    /// Client token of the API client.
    /// </summary>
    public string ClientToken { get; set; } = null!;

    /// <summary>
    /// Client secret of the API client. Signs the request.
    /// </summary>
    public string ClientSecret { get; set; } = null!;

    /// <summary>
    /// Access token of the API client.
    /// </summary>
    public string AccessToken { get; set; } = null!;
}
