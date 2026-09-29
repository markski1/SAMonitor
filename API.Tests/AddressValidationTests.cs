using System.Net;
using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

public sealed class AddressValidationTests
{
    [Theory]
    [InlineData("1.2.3.999:7777")]
    [InlineData("1.2.3.4:0")]
    [InlineData("1.2.3.4:65536")]
    [InlineData("1.2.3.4:abc")]
    [InlineData("::1:7777")]
    public async Task MalformedAddresses_AreRejected(string address)
    {
        Assert.Equal("invalid", await Helpers.ValidateIPv4(address));
    }

    [Fact]
    public async Task Addresses_AreCanonicalizedAndDnsUsesIPv4()
    {
        Assert.Equal("1.2.3.4:7777", await Helpers.ValidateIPv4(" 001.002.003.004:07777 "));
        Assert.Equal("1.2.3.4:7777", await Helpers.ValidateIPv4("1.2.3.4"));
        Assert.Equal("127.0.0.1:7788", await Helpers.ValidateIPv4("localhost:7788",
            _ => Task.FromResult(new[] { IPAddress.IPv6Loopback, IPAddress.Loopback })));
        Assert.Equal("invalid", await Helpers.ValidateIPv4("localhost",
            _ => Task.FromResult(new[] { IPAddress.IPv6Loopback })));
    }
}
