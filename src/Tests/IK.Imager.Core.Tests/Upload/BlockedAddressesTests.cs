using System.Net;
using IK.Imager.Core.Upload;
using Xunit;

namespace IK.Imager.Core.Tests.Upload;

/// <summary>
/// The list a caller supplied url is checked against before the service connects to it.
/// </summary>
public class BlockedAddressesTests
{
    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("10.0.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.254")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("ff02::1")]
    public void Contains_AddressThatIsNotPubliclyRoutable_ReturnsTrue(string address) =>
        Assert.True(BlockedAddresses.Contains(IPAddress.Parse(address)));

    //the same addresses written so that a check on the v4 form alone would miss them
    [Theory]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("64:ff9b::a00:1")]
    public void Contains_PrivateAddressReachedThroughIPv6_ReturnsTrue(string address) =>
        Assert.True(BlockedAddresses.Contains(IPAddress.Parse(address)));

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.1")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.1")]
    [InlineData("2606:4700:4700::1111")]
    public void Contains_PubliclyRoutableAddress_ReturnsFalse(string address) =>
        Assert.False(BlockedAddresses.Contains(IPAddress.Parse(address)));
}
