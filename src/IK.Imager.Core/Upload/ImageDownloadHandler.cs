using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace IK.Imager.Core.Upload;

/// <summary>
/// Builds the primary handler of the image download client. A factory rather than a subclass, because
/// SocketsHttpHandler is sealed.
///
/// The handler resolves the host itself, refuses every address <see cref="BlockedAddresses"/> lists, and
/// then connects to the addresses it has just checked. Letting the socket resolve the name a second time
/// would leave a window for a name that answers with a public address first and a private one a moment later.
///
/// Redirects are off on purpose - <see cref="ImageDownloader"/> follows them a hop at a time, so every hop
/// passes through this handler and the chain stays bounded.
/// </summary>
public static class ImageDownloadHandler
{
    public static SocketsHttpHandler Create(bool allowPrivateAddresses = false) =>
        new()
        {
            AllowAutoRedirect = false,
            ConnectCallback = (context, cancellationToken) =>
                Connect(context, allowPrivateAddresses, cancellationToken)
        };

    private static async ValueTask<Stream> Connect(SocketsHttpConnectionContext context,
        bool allowPrivateAddresses, CancellationToken cancellationToken)
    {
        var endPoint = context.DnsEndPoint;
        var addresses = await Dns.GetHostAddressesAsync(endPoint.Host, cancellationToken);

        if (!allowPrivateAddresses)
        {
            //one blocked address is enough to refuse the host - a name that answers with both a public and a
            //private address is the whole point of a rebinding attack
            foreach (var address in addresses)
                if (BlockedAddresses.Contains(address))
                    throw new HttpRequestException(
                        $"{endPoint.Host} resolves to {address}, which this service is not allowed to download from.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, endPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
