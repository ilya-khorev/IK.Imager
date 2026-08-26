using System;
using System.Net;
using System.Net.Sockets;

namespace IK.Imager.Core.Upload;

/// <summary>
/// The addresses upload-by-url must never fetch from. The caller chooses the url, so without this a request
/// for http://169.254.169.254/ reads the cloud metadata endpoint from inside the deployment, and a private
/// address reaches whatever else is only meant to be reachable from in there.
///
/// The list is the IANA special purpose blocks. None of them is routable on the internet, so an image the
/// service is meant to fetch is never at one of these addresses.
/// </summary>
public static class BlockedAddresses
{
    private static readonly (uint Network, int PrefixLength)[] BlockedIPv4 =
    [
        (Ip(0, 0, 0, 0), 8),        //"this network"
        (Ip(10, 0, 0, 0), 8),       //private
        (Ip(100, 64, 0, 0), 10),    //carrier grade NAT
        (Ip(127, 0, 0, 0), 8),      //loopback
        (Ip(169, 254, 0, 0), 16),   //link local - where the cloud metadata endpoints live
        (Ip(172, 16, 0, 0), 12),    //private
        (Ip(192, 0, 0, 0), 24),     //IETF protocol assignments
        (Ip(192, 0, 2, 0), 24),     //documentation
        (Ip(192, 88, 99, 0), 24),   //6to4 relay anycast
        (Ip(192, 168, 0, 0), 16),   //private
        (Ip(198, 18, 0, 0), 15),    //benchmarking
        (Ip(198, 51, 100, 0), 24),  //documentation
        (Ip(203, 0, 113, 0), 24),   //documentation
        (Ip(224, 0, 0, 0), 4),      //multicast
        (Ip(240, 0, 0, 0), 4)       //reserved, and the broadcast address with it
    ];

    public static bool Contains(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        //::ffff:169.254.169.254 is the metadata endpoint again, so the v4 address is what has to be checked
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsBlockedIPv4(address),
            AddressFamily.InterNetworkV6 => IsBlockedIPv6(address),
            //not IP at all, so not something to open a connection to
            _ => true
        };
    }

    private static bool IsBlockedIPv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var value = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

        foreach (var (network, prefixLength) in BlockedIPv4)
        {
            var mask = uint.MaxValue << (32 - prefixLength);
            if ((value & mask) == network)
                return true;
        }

        return false;
    }

    private static bool IsBlockedIPv6(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.IPv6Any))
            return true;

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal ||
            address.IsIPv6Multicast || address.IsIPv6Teredo)
            return true;

        //64:ff9b::/96 carries an IPv4 address through a NAT64 gateway, which is one more way to ask for a
        //private v4 network
        var bytes = address.GetAddressBytes();
        return bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xff && bytes[3] == 0x9b &&
               !bytes.AsSpan(4, 8).ContainsAnyExcept((byte)0);
    }

    private static uint Ip(byte a, byte b, byte c, byte d) =>
        ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
}
