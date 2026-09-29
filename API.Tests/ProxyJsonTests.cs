using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ProxyJsonTests
{
    [Fact]
    public void QueryResponse_AcceptsLowercaseProxyKeys()
    {
        const string json = """
            {"info":{"hostname":"My Server","players":123},"rules":{"lagcomp":true,"version":"omp 1.4","weburl":"example.com"}}
            """;

        var result = ProxyJson.DeserializeQueryResponse(json);

        Assert.NotNull(result?.Info);
        Assert.NotNull(result.Rules);
        Assert.Equal("My Server", result.Info.HostName);
        Assert.Equal((ushort)123, result.Info.Players);
        Assert.True(result.Rules.LagComp);
        Assert.Equal("omp 1.4", result.Rules.Version);
        Assert.Equal("example.com", result.Rules.ToServerRules("English").WebUrl?.ToString());
    }

    [Fact]
    public void PlayersResponse_ParsesPlayerFields()
    {
        const string json = """
            {"players":[{"PlayerId":7,"PlayerName":"Alice","PlayerScore":99,"PlayerPing":42}]}
            """;

        var result = ProxyJson.DeserializePlayersResponse(json);

        Assert.NotNull(result?.Players);
        var player = Assert.Single(result.Players);
        Assert.Equal((byte)7, player.PlayerId);
        Assert.Equal("Alice", player.PlayerName);
        Assert.Equal(99, player.PlayerScore);
        Assert.Equal(42, player.PlayerPing);
    }
}
