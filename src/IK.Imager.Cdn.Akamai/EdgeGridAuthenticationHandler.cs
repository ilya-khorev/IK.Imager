using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace IK.Imager.Cdn.Akamai;

/// <summary>
/// Signs every outgoing request with <see cref="EdgeGridSigner"/>.
/// </summary>
public class EdgeGridAuthenticationHandler(IOptions<AkamaiCdnSettings> settings) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        //read before signing - the signature covers these exact bytes
        var body = request.Content == null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        request.Headers.TryAddWithoutValidation("Authorization", EdgeGridSigner.CreateAuthorizationHeader(
            settings.Value, request, body, DateTimeOffset.UtcNow, Guid.NewGuid().ToString()));

        return await base.SendAsync(request, cancellationToken);
    }
}
