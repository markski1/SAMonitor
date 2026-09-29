using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

public sealed class QueryNormalizationTests
{
    [Fact]
    public void NonRussianQueries_RepairLatinText()
    {
        var info = SqHelpers.NormalizeServerInfo(new ServerInfo
        {
            HostName = "Los сaballeros", GameMode = "Stкnt", Language = "Spanish"
        });
        var rules = SqHelpers.NormalizeServerRules(new ServerRules { MapName = "Desкrt" }, "English");

        Assert.Equal("Los ñaballeros", info.HostName);
        Assert.Equal("Stênt", info.GameMode);
        Assert.Equal("Desêrt", rules.MapName);
    }

    [Fact]
    public void RussianQueries_PreserveCyrillicText()
    {
        var info = SqHelpers.NormalizeServerInfo(new ServerInfo
        {
            HostName = "Русский сервер", Language = "Русский"
        });

        Assert.Equal("Русский сервер", info.HostName);
    }
}
