using System.Text.Json.Serialization;
using SAMonitor.Utils;

namespace SAMonitor.Data;

// Internal use classes.

[Flags]
internal enum ServerFilterPreset
{
    None = 0,
    OnlyOpenMp = 1,
    HideEmpty = 2,
    HidePassworded = 4,
    HideRoleplay = 8,
    RequireSampCac = 16
}

internal static class ServerFilterLogic
{
    internal static ServerFilterPreset BuildPreset(bool showEmpty, bool showPassworded, bool hideRoleplay, bool requireSampCac, bool onlyOpenMp)
    {
        ServerFilterPreset preset = ServerFilterPreset.None;

        if (onlyOpenMp) preset |= ServerFilterPreset.OnlyOpenMp;
        if (!showEmpty) preset |= ServerFilterPreset.HideEmpty;
        if (!showPassworded) preset |= ServerFilterPreset.HidePassworded;
        if (hideRoleplay) preset |= ServerFilterPreset.HideRoleplay;
        if (requireSampCac) preset |= ServerFilterPreset.RequireSampCac;

        return preset;
    }

    internal static bool MatchesPreset(Server server, ServerFilterPreset preset)
    {
        if ((preset & ServerFilterPreset.OnlyOpenMp) != 0 && !server.IsOpenMp) return false;
        if ((preset & ServerFilterPreset.HideEmpty) != 0 && server.PlayersOnline <= 0) return false;
        if ((preset & ServerFilterPreset.HidePassworded) != 0 && server.RequiresPassword) return false;
        if ((preset & ServerFilterPreset.HideRoleplay) != 0 && IsRoleplay(server)) return false;
        if ((preset & ServerFilterPreset.RequireSampCac) != 0 && server.SampCac.Contains("not required", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    internal static bool IsRoleplay(Server server)
    {
        return server.GameMode.Contains("rp", StringComparison.OrdinalIgnoreCase) ||
               server.GameMode.Contains("role", StringComparison.OrdinalIgnoreCase) ||
               server.Name.Contains("roleplay", StringComparison.OrdinalIgnoreCase) ||
               server.Name.Contains("role play", StringComparison.OrdinalIgnoreCase);
    }

    internal static List<Server> ApplyTextFilters(IReadOnlyList<Server> source, string name, string version, string language, string gamemode)
    {
        List<Server> servers = [.. source];

        if (name != "unspecified")
        {
            servers = servers.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (version != "any")
        {
            servers = servers.Where(x => x.Version.Contains(version, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // In the future, should probably have a way to specify a language in a more broad sense rather than by string,
        // as server operators define languages in rather inconsistent ways.
        if (language != "any")
        {
            servers = servers.Where(x => x.Language.Contains(language, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (gamemode != "unspecified")
        {
            servers = servers.Where(x => x.GameMode.Contains(gamemode, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return servers;
    }

    internal static List<Server> ApplyOrdering(List<Server> servers, string order, bool showEmpty)
    {
        if (order != "none")
        {
            if (order == "players")
            {
                return servers.OrderByDescending(x => x.PlayersOnline).ToList();
            }

            // show_empty=0 guarantees PlayersOnline will never be zero.
            // otherwise we have to separate them
            if (!showEmpty)
            {
                return servers.OrderBy(x => (double)x.MaxPlayers / x.PlayersOnline).ToList();
            }

            var emptyServers = servers.Where(x => x.PlayersOnline == 0);
            var populatedServers = servers.Where(x => x.PlayersOnline > 0);

            servers = populatedServers.OrderByDescending(x => (double)x.PlayersOnline / x.MaxPlayers).ToList();
            servers.AddRange(emptyServers);
            return servers;
        }

        // if "none", then order by the ShuffleOrder, which gets shuffled every 30 minutes.
        return servers.OrderBy(x => x.ShuffledOrder).ToList();
    }
}

internal sealed class ServerFilterSnapshot
{
    private const int PresetCount = 32;
    private readonly Dictionary<int, List<Server>> _bases;

    private ServerFilterSnapshot(Dictionary<int, List<Server>> bases)
    {
        _bases = bases;
    }

    internal static ServerFilterSnapshot Create(IReadOnlyList<Server> servers)
    {
        var bases = new Dictionary<int, List<Server>>(PresetCount);
        for (int mask = 0; mask < PresetCount; mask++)
        {
            bases[mask] = [];
        }

        foreach (var server in servers)
        {
            for (int mask = 0; mask < PresetCount; mask++)
            {
                if (ServerFilterLogic.MatchesPreset(server, (ServerFilterPreset)mask))
                {
                    bases[mask].Add(server);
                }
            }
        }

        return new ServerFilterSnapshot(bases);
    }

    internal List<Server> GetPreset(ServerFilterPreset preset)
    {
        return [.. _bases[(int)preset]];
    }
}

public class ServerFilterer
{
    private bool ShowEmpty { get; set; }
    private bool ShowPassworded { get; set; }
    private bool HideRoleplay { get; set; }
    private bool RequireSampCac { get; set; }
    private bool OnlyOpenMp { get; set; }
    private string Name { get; set; }
    private string Gamemode { get; set; }
    private string Version { get; set; }
    private string Language { get; set; }
    private string Order { get; set; }

    public ServerFilterer(bool showEmpty, bool showPassworded, bool hideRoleplay, bool requireSampCac, string name, string gamemode, string version, string language, string order, bool onlyOpenMp)
    {
        ShowEmpty = showEmpty;
        ShowPassworded = showPassworded;
        HideRoleplay = hideRoleplay;
        RequireSampCac = requireSampCac;
        OnlyOpenMp = onlyOpenMp;
        Name = name;
        Gamemode = gamemode;
        Version = version;
        Language = language;
        Order = order;
        OnlyOpenMp = onlyOpenMp;
    }

    /*
     * People who hate if statements for no reason beware:
     *
     * I don't care that there's "prettier" ways to do this. I want this to stay fast.
     *
     * The cheap boolean filters (empty/passworded/roleplay/open.mp/SAMPCAC) are precomputed
     * into cached base lists in ServerManager. Per request, we only apply the free-text
     * filters and ordering over the smallest viable base list.
     */
    public List<Server> GetFilteredServers()
    {
        var preset = ServerFilterLogic.BuildPreset(ShowEmpty, ShowPassworded, HideRoleplay, RequireSampCac, OnlyOpenMp);
        List<Server> servers = ServerManager.GetFilteredServerBase(preset);

        servers = ServerFilterLogic.ApplyTextFilters(servers, Name, Version, Language, Gamemode);
        servers = ServerFilterLogic.ApplyOrdering(servers, Order, ShowEmpty);

        return servers;
    }
}


// API Return classes.
public class Player(ServerPlayer player)
{
    public int Id { get; set; } = player.PlayerId;
    public int Ping { get; set; } = player.PlayerPing;
    public string Name { get; set; } = player.PlayerName;
    public int Score { get; set; } = player.PlayerScore;
}

public class GlobalMetrics(int players, int servers, int ompServers, DateTime time)
{
    public int Players { get; init; } = players;
    public int Servers { get; init; } = servers;
    public int OmpServers { get; init; } = ompServers;
    public DateTime Time { get; init; } = time;
}
public class ServerMetrics(int players, DateTime time)
{
    public int Players { get; init; } = players;
    public DateTime Time { get; init; } = time;
}

public class GlobalStats(int playersOnline, int serversTracked, int serversInhabited, int serversOnline, int serversOnlineOmp)
{
    public int PlayersOnline { get; set; } = playersOnline;
    public int ServersTracked { get; set; } = serversTracked;
    public int ServersOnline { get; set; } = serversOnline;
    public int ServersInhabited { get; set; } = serversInhabited;
    [JsonPropertyName("serversOnlineOMP")] // Unfortunately mistakes were made. This is the published name in the API and compat cannot be broken.
    public int ServersOnlineOmp { get; set; } = serversOnlineOmp;
}

public class CategoryStats
{
    public int Amount { get; set; } = 0;
    public int Players { get; set; } = 0;

    public void Add(Server server)
    {
        Amount++;
        Players += server.PlayersOnline;
    }
}

public class LanguageStats
{
    public CategoryStats Spanish { get; set; } = new();
    public CategoryStats Russian { get; set; } = new();
    public CategoryStats English { get; set; } = new();
    public CategoryStats Romanian { get; set; } = new();
    public CategoryStats Portuguese { get; set; } = new();
    public CategoryStats Asia { get; set; } = new();
    public CategoryStats EastEuro { get; set; } = new();
    public CategoryStats WestEuro { get; set; } = new();
    public CategoryStats Other { get; set; } = new();
}

public class GamemodeStats
{
    public CategoryStats Deathmatch { get; set; } = new();
    public CategoryStats Roleplay { get; set; } = new();
    public CategoryStats RaceStunt { get; set; } = new();
    public CategoryStats Cnr { get; set; } = new();
    public CategoryStats FreeRoam { get; set; } = new();
    public CategoryStats Survival { get; set; } = new();
    public CategoryStats VehSim { get; set; } = new();
    public CategoryStats Other { get; set; } = new();
}