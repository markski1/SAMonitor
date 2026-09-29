using SAMonitor.Data;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ServerFilterLogicTests
{
    [Fact]
    public void Preset_CombinesServerFilters()
    {
        using var match = CreateServer();
        using var empty = CreateServer();
        using var passworded = CreateServer();
        using var roleplay = CreateServer();
        using var noCac = CreateServer();
        using var classic = CreateServer();
        empty.PlayersOnline = 0;
        passworded.RequiresPassword = true;
        roleplay.GameMode = "Roleplay";
        noCac.SampCac = "Not required";
        classic.IsOpenMp = false;
        var snapshot = ServerFilterSnapshot.Create([match, empty, passworded, roleplay, noCac, classic]);
        var preset = ServerFilterLogic.BuildPreset(
            showEmpty: false, showPassworded: false, hideRoleplay: true, requireSampCac: true, onlyOpenMp: true);

        Assert.Same(match, Assert.Single(snapshot.GetPreset(preset)));
    }

    [Fact]
    public void RatioOrdering_PutsFullerServersFirstAndEmptyServersLast()
    {
        using var full = CreateServer();
        using var half = CreateServer();
        using var empty = CreateServer();
        full.PlayersOnline = 80;
        half.PlayersOnline = 50;
        empty.PlayersOnline = 0;

        Assert.Equal([full, half, empty],
            ServerFilterLogic.ApplyOrdering([half, empty, full], "ratio", showEmpty: true));
    }

    [Fact]
    public void TextFilters_CombineCaseInsensitiveMatches()
    {
        using var match = CreateServer();
        using var otherLanguage = CreateServer();
        using var otherMode = CreateServer();
        using var otherVersion = CreateServer();
        using var otherName = CreateServer();
        otherLanguage.Language = "English";
        otherMode.GameMode = "Freeroam";
        otherVersion.Version = "0.3.7";
        otherName.Name = "Other server";

        var result = ServerFilterLogic.ApplyTextFilters(
            [match, otherLanguage, otherMode, otherVersion, otherName], "brazil", "omp", "port", "drift");

        Assert.Same(match, Assert.Single(result));
    }

    private static Server CreateServer() => new("203.0.113.1:7777")
    {
        Name = "Brazil Drift Arena",
        GameMode = "Drift",
        Language = "Portuguese",
        PlayersOnline = 25,
        MaxPlayers = 100,
        IsOpenMp = true,
        SampCac = "1.0.0",
        Version = "omp 1.4"
    };
}
