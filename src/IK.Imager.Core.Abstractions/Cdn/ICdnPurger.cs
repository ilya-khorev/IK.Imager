using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions.Cdn;

/// <summary>
/// Removes content from a CDN cache after it has been deleted from the origin.
/// Deleting a blob does not clear the edge cache, so the CDN keeps serving the image until its TTL expires.
/// </summary>
/// <remarks>
/// The contract is the intersection of what CDNs support, so that a new provider is one class:
/// <list type="bullet">
/// <item><description>
/// Uris are absolute and already on the CDN host. Cloudflare, Akamai and Fastly purge by full url;
/// Azure Front Door and CloudFront take a path, which is <see cref="Uri.AbsolutePath"/>.
/// </description></item>
/// <item><description>
/// Uris arrive as a batch because providers rate limit the request, not the uri. Splitting a batch down
/// to a provider's own limit (30 urls per Cloudflare call, 3000 paths per CloudFront invalidation)
/// belongs in the implementation.
/// </description></item>
/// <item><description>
/// The task completes when the purge is accepted, not when it has propagated. Every provider runs this
/// asynchronously over minutes.
/// </description></item>
/// <item><description>
/// Failures throw. The caller runs in a Service Bus consumer, so an exception means a retry and then a
/// dead letter. A swallowed failure would leave a deleted image on the edge with only a log line.
/// </description></item>
/// </list>
/// Core registers <c>NoOpCdnPurger</c> with <c>TryAddSingleton</c>, so a provider module can register
/// its own implementation instead.
/// </remarks>
public interface ICdnPurger
{
    /// <summary>
    /// Asks the CDN to drop its cached copies of the given uris.
    /// </summary>
    /// <param name="contentUris">
    /// Absolute uris on the CDN host, as produced by <see cref="IImageUrlBuilder"/>.
    /// An empty collection does nothing.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    Task Purge(IReadOnlyCollection<Uri> contentUris, CancellationToken cancellationToken);
}
