using SAMonitor.Data;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ServerFilterLogicTests
{
    [Fact]
    public void SnapshotPreset_UsesCachedBaseFiltersBeforeTextFilters()
    {
        var baseline = CreateServer("alpha.example", name: "Alpha Freeroam", gameMode: "Freeroam", language: "English", playersOnline: 42, maxPlayers: 100, isOpenMp: true, sampCac: "1.0.0");
        var empty = CreateServer("empty.example", name: "Empty Server", gameMode: "Freeroam", language: "English", playersOnline: 0, maxPlayers: 100, isOpenMp: true, sampCac: "1.0.0");
        var passworded = CreateServer("pw.example", name: "Pw Server", gameMode: "Freeroam", language: "English", playersOnline: 10, maxPlayers: 100, isOpenMp: true, requiresPassword: true, sampCac: "1.0.0");
        var roleplay = CreateServer("rp.example", name: "Roleplay World", gameMode: "Roleplay", language: "English", playersOnline: 20, maxPlayers: 100, isOpenMp: true, sampCac: "1.0.0");
        var noCac = CreateServer("nocac.example", name: "No CAC", gameMode: "Freeroam", language: "English", playersOnline: 15, maxPlayers: 100, isOpenMp: true, sampCac: "Not required");
        var notOmp = CreateServer("samp.example", name: "Classic SA-MP", gameMode: "Freeroam", language: "English", playersOnline: 30, maxPlayers: 100, isOpenMp: false, sampCac: "1.0.0");

        var snapshot = ServerFilterSnapshot.Create([baseline, empty, passworded, roleplay, noCac, notOmp]);
        var preset = ServerFilterLogic.BuildPreset(showEmpty: false, showPassworded: false, hideRoleplay: true, requireSampCac: true, onlyOpenMp: true);

        var result = snapshot.GetPreset(preset);

        Assert.Collection(result, server => Assert.Same(baseline, server));
    }

    [Fact]
    public void ApplyOrdering_RatioWithShowEmpty_LeavesEmptyServersAtEnd()
    {
        var almostFull = CreateServer("a.example", name: "Almost Full", gameMode: "Freeroam", language: "English", playersOnline: 80, maxPlayers: 100);
        var halfFull = CreateServer("b.example", name: "Half Full", gameMode: "Freeroam", language: "English", playersOnline: 50, maxPlayers: 100);
        var empty = CreateServer("c.example", name: "Empty", gameMode: "Freeroam", language: "English", playersOnline: 0, maxPlayers: 100);

        var ordered = ServerFilterLogic.ApplyOrdering([almostFull, empty, halfFull], order: "ratio", showEmpty: true);

        Assert.Equal([almostFull, halfFull, empty], ordered);
    }

    [Fact]
    public void ApplyTextFilters_FiltersNameVersionLanguageAndGamemode()
    {
        var match = CreateServer("match.example", name: "Brazil Drift Arena", gameMode: "Drift", language: "Portuguese", playersOnline: 25, maxPlayers: 100, version: "omp 1.4.0.2783");
        var noLanguage = CreateServer("lang.example", name: "Brazil Drift Arena", gameMode: "Drift", language: "English", playersOnline: 25, maxPlayers: 100, version: "omp 1.4.0.2783");
        var noGamemode = CreateServer("gm.example", name: "Brazil Drift Arena", gameMode: "Freeroam", language: "Portuguese", playersOnline: 25, maxPlayers: 100, version: "omp 1.4.0.2783");
        var noVersion = CreateServer("ver.example", name: "Brazil Drift Arena", gameMode: "Drift", language: "Portuguese", playersOnline: 25, maxPlayers: 100, version: "0.3.7");

        var filtered = ServerFilterLogic.ApplyTextFilters([match, noLanguage, noGamemode, noVersion], name: "brazil", version: "omp", language: "port", gamemode: "drift");

        Assert.Collection(filtered, server => Assert.Same(match, server));
    }

    private static Server CreateServer(
        string ipAddr,
        string name,
        string gameMode,
        string language,
        int playersOnline,
        int maxPlayers,
        bool isOpenMp = true,
        bool requiresPassword = false,
        string sampCac = "1.0.0",
        string version = "omp 1.4.0.2783")
    {
        return new Server(ipAddr)
        {
            Name = name,
            GameMode = gameMode,
            Language = language,
            PlayersOnline = playersOnline,
            MaxPlayers = maxPlayers,
            IsOpenMp = isOpenMp,
            RequiresPassword = requiresPassword,
            SampCac = sampCac,
            Version = version
        };
    }
}
