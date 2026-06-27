using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ProxyJsonTests
{
    [Fact]
    public void DeserializeQueryResponse_ParsesLowercaseProxyPayload()
    {
        const string json = """
        {
          "info": {
            "HostName": "My Server",
            "Players": 123,
            "MaxPlayers": 500,
            "GameMode": "Freeroam",
            "Language": "English",
            "Password": false
          },
          "rules": {
            "LagComp": true,
            "MapName": "San Andreas",
            "Version": "omp 1.4.0.2783",
            "SampcacVersion": "1.0.0",
            "Weather": 10,
            "WebUrl": "https://example.com",
            "WorldTime": "12:34"
          }
        }
        """;

        var result = ProxyJson.DeserializeQueryResponse(json);

        Assert.NotNull(result);
        Assert.NotNull(result!.Info);
        Assert.NotNull(result.Rules);
        Assert.Equal("My Server", result.Info!.HostName);
        Assert.Equal((ushort)123, result.Info.Players);
        Assert.Equal((ushort)500, result.Info.MaxPlayers);
        Assert.Equal("Freeroam", result.Info.GameMode);
        Assert.Equal("English", result.Info.Language);
        Assert.False(result.Info.Password);
        Assert.True(result.Rules!.LagComp);
        Assert.Equal("San Andreas", result.Rules.MapName);
        Assert.Equal("omp 1.4.0.2783", result.Rules.Version);
        Assert.Equal("1.0.0", result.Rules.SampcacVersion);
        Assert.Equal(10, result.Rules.Weather);
        Assert.Equal("https://example.com", result.Rules.WebUrl);
        Assert.Equal("12:34", result.Rules.WorldTime);
    }

    [Fact]
    public void DeserializePlayersResponse_ParsesPlayerList()
    {
        const string json = """
        {
          "players": [
            {
              "PlayerId": 7,
              "PlayerName": "Alice",
              "PlayerScore": 99,
              "PlayerPing": 42
            },
            {
              "PlayerId": 8,
              "PlayerName": "Bob",
              "PlayerScore": 123,
              "PlayerPing": 55
            }
          ]
        }
        """;

        var result = ProxyJson.DeserializePlayersResponse(json);

        Assert.NotNull(result);
        Assert.NotNull(result!.Players);
        Assert.Collection(result.Players!,
            player =>
            {
                Assert.Equal((byte)7, player.PlayerId);
                Assert.Equal("Alice", player.PlayerName);
                Assert.Equal(99, player.PlayerScore);
                Assert.Equal(42, player.PlayerPing);
            },
            player =>
            {
                Assert.Equal((byte)8, player.PlayerId);
                Assert.Equal("Bob", player.PlayerName);
                Assert.Equal(123, player.PlayerScore);
                Assert.Equal(55, player.PlayerPing);
            });
    }
}
