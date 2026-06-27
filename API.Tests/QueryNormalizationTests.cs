using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

public sealed class QueryNormalizationTests
{
    [Fact]
    public void NormalizeServerInfo_FixesKnownLatinCharacters_ForNonRussianServers()
    {
        var info = new ServerInfo
        {
            HostName = "Los сaballeros",
            GameMode = "Stкnt",
            Language = "Spanish"
        };

        var normalized = SqHelpers.NormalizeServerInfo(info);

        Assert.Equal("Los ñaballeros", normalized.HostName);
        Assert.Equal("Stênt", normalized.GameMode);
        Assert.Equal("Spanish", normalized.Language);
    }

    [Fact]
    public void NormalizeServerInfo_LeavesRussianServersUntouched()
    {
        var info = new ServerInfo
        {
            HostName = "Русский сервер",
            GameMode = "Roleplay",
            Language = "Русский"
        };

        var normalized = SqHelpers.NormalizeServerInfo(info);

        Assert.Same(info, normalized);
    }

    [Fact]
    public void NormalizeServerRules_FixesMapName_ForNonRussianServers()
    {
        var rules = new ServerRules
        {
            MapName = "Desкrt"
        };

        var normalized = SqHelpers.NormalizeServerRules(rules, "English");

        Assert.Same(rules, normalized);
        Assert.Equal("Desêrt", normalized.MapName);
    }
}
