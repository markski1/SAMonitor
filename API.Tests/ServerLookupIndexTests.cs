using SAMonitor.Data;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ServerLookupIndexTests
{
    [Fact]
    public void Lookup_UsesExactPortThenDefaultPort()
    {
        using var alternate = new Server("1.2.3.4:7788");
        using var primary = new Server("1.2.3.4:7777");
        using var similar = new Server("11.2.3.4:7777");
        var index = ServerLookupIndex.Create([alternate, primary, similar]);

        Assert.Same(alternate, index.Lookup("1.2.3.4:7788"));
        Assert.Same(primary, index.Lookup("1.2.3.4"));
        Assert.Null(index.Lookup("5.6.7.8"));
        Assert.Null(ServerLookupIndex.Create([similar]).Lookup("1.2.3.4:7777"));
    }
}
