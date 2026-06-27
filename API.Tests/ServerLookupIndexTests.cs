using SAMonitor.Data;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ServerLookupIndexTests
{
    [Fact]
    public void Lookup_ReturnsExactIpMatch_WhenPortIsSpecified()
    {
        var primary = CreateServer("1.2.3.4:7777");
        var alternate = CreateServer("1.2.3.4:7788");
        var index = ServerLookupIndex.Create([primary, alternate]);

        var result = index.Lookup("1.2.3.4:7788");

        Assert.Same(alternate, result);
    }

    [Fact]
    public void Lookup_PrefersDefaultPort_WhenOnlyHostIsSpecified()
    {
        var alternate = CreateServer("1.2.3.4:7788");
        var primary = CreateServer("1.2.3.4:7777");
        var index = ServerLookupIndex.Create([alternate, primary]);

        var result = index.Lookup("1.2.3.4");

        Assert.Same(primary, result);
    }

    [Fact]
    public void Lookup_ReturnsNull_ForUnknownHost()
    {
        var index = ServerLookupIndex.Create([CreateServer("1.2.3.4:7777")]);

        Assert.Null(index.Lookup("5.6.7.8"));
    }

    private static Server CreateServer(string ipAddr)
    {
        return new Server(ipAddr)
        {
            Name = ipAddr,
            GameMode = "Freeroam",
            Language = "English",
            Version = "omp 1.4.0.2783",
            SampCac = "1.0.0"
        };
    }
}
