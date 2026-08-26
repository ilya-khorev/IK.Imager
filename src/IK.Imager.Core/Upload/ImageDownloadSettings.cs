using System;

namespace IK.Imager.Core.Upload;

/// <summary>
/// What the service is allowed to do while fetching an image the caller only gave a url for.
/// </summary>
public class ImageDownloadSettings
{
    /// <summary>
    /// Bounds the whole download, including any retry the host wraps around it - HttpClient.Timeout covers
    /// the entire pipeline rather than one attempt. Without it the client waits the 100 second default,
    /// which is what a server answering a byte a second would take.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How many redirects one download may follow. Every hop is checked again, so this only bounds the chain.
    /// </summary>
    public int MaxRedirects { get; set; } = 5;

    /// <summary>
    /// Lets the service download from loopback, link local and private addresses. Off by default: the caller
    /// chooses the url, so anything reachable only from inside the deployment would be reachable through this
    /// endpoint. Turn it on only where the image sources really are internal.
    /// </summary>
    public bool AllowPrivateAddresses { get; set; }
}
