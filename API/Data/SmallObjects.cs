using SAMPQuery;

namespace SAMonitor.Data;

// Internal use classes.

public class ServerFilterer
{
    public bool ShowEmpty { get; set; }
    public bool ShowPassworded { get; set; }
    public bool HideRoleplay { get; set; }
    public bool RequireSampCAC { get; set; }
    public bool OnlyOpenMp { get; set; }
    public string Name { get; set; }
    public string Gamemode { get; set; }
    public string Version { get; set; }
    public string Language { get; set; }
    public string Order { get; set; }

    public ServerFilterer(bool showEmpty, bool showPassworded, bool hideRoleplay, bool requireSampCAC, string name, string gamemode, string version, string language, string order, bool onlyOpenMp)
    {
        ShowEmpty = showEmpty;
        ShowPassworded = showPassworded;
        HideRoleplay = hideRoleplay;
        RequireSampCAC = requireSampCAC;
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
     * I don't care that there's "prettier" ways to do this. I don't want this to be pretty, I want it to be performant.
     * 
     * Don't tell me about all the fancy ways this could be refactored to be "pretty", unless they run as fast as this does.
     * 
     * Also, it might seem counter-intuitive, but using ToList() after every filter is actually faster than just doing it once at the end.
     * If you don't believe me, look into how lambda expression trees work in LINQ. It's rather trippy.
     */
    public List<Server> GetFilteredServers()
    {
        List<Server> servers = ServerManager.GetServers();

        if (OnlyOpenMp)
        {
            servers = servers.Where(x => x.IsOpenMp).ToList();
        }

        if (!ShowEmpty)
        {
            servers = servers.Where(x => x.PlayersOnline > 0).ToList();
        }

        if (!ShowPassworded)
        {
            servers = servers.Where(x => x.RequiresPassword == false).ToList();
        }

        if (HideRoleplay)
        {
            servers = servers.Where(x => !x.GameMode.Contains("rp", StringComparison.OrdinalIgnoreCase) &&
                                         !x.GameMode.Contains("role", StringComparison.OrdinalIgnoreCase) &&
                                         !x.Name.Contains("roleplay", StringComparison.OrdinalIgnoreCase) &&
                                         !x.Name.Contains("role play", StringComparison.OrdinalIgnoreCase))
                             .ToList();
        }

        if (RequireSampCAC)
        {
            servers = servers.Where(x => !x.SampCac.Contains("not required", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (Name != "unspecified")
        {
            servers = servers.Where(x => x.Name.Contains(Name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (Version != "any")
        {
            servers = servers.Where(x => x.Version.Contains(Version, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // In the future, should probably have a way to specify a language in a more broad sense rather than by string,
        // as server operators define languages in rather inconsistent ways.
        if (Language != "any")
        {
            servers = servers.Where(x => x.Language.Contains(Language, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (Gamemode != "unspecified")
        {
            servers = servers.Where(x => x.GameMode.Contains(Gamemode, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // if specified, order
        if (Order != "none")
        {
            // by player count
            if (Order == "players")
            {
                servers = servers.OrderByDescending(x => x.PlayersOnline).ToList();
            }
            // by player count over max player ratio.
            else
            {
                // show_empty=0 guarantees PlayersOnline will never be zero.
                // otherwise we have to separate them
                if (!ShowEmpty)
                {
                    servers = servers.OrderBy(x => x.MaxPlayers / x.PlayersOnline).ToList();
                }
                else
                {
                    var emptyServers = servers.Where(x => x.PlayersOnline == 0);
                    var populatedServers = servers.Where(x => x.PlayersOnline > 0);

                    servers = populatedServers.OrderByDescending(x => x.PlayersOnline / x.MaxPlayers).ToList();
                    servers.AddRange(emptyServers);
                }
            }
        }
        else
        {
            // if "none", then order by the ShuffleOrder which gets shuffled every 30 minutes.
            servers = servers.OrderBy(x => x.ShuffledOrder).ToList();
        }

        return servers;
    }
}


// API Return classes.
public class Player
{
    public int Id { get; set; }
    public int Ping { get; set; }
    public string Name { get; set; }
    public int Score { get; set; }
    public Player(ServerPlayer player)
    {
        Id = player.PlayerId;
        Ping = player.PlayerPing;
        Name = player.PlayerName ?? "Unknown";
        Score = player.PlayerScore;
    }
}
public class GlobalMetrics
{
    public int Players { get; set; }
    public int Servers { get; set; }
    public int ApiHits { get; set; }
    public DateTime Time { get; set; }

    public GlobalMetrics(int players, int servers, int api_hits, DateTime time)
    {
        Players = players;
        Servers = servers;
        ApiHits = api_hits;
        Time = time;
    }
}
public class ServerMetrics
{
    public int Players { get; set; }
    public DateTime Time { get; set; }

    public ServerMetrics(int players, DateTime time)
    {
        Players = players;
        Time = time;
    }
}

public class GlobalStats
{
    public int PlayersOnline { get; set; }
    public int ServersTracked { get; set; }
    public int ServersOnline { get; set; }
    public int ServersOnlineOMP { get; set; }

    public GlobalStats(int playersOnline, int serversTracked, int serversOnline, int serversOnlineOMP)
    {
        PlayersOnline = playersOnline;
        ServersTracked = serversTracked;
        ServersOnline = serversOnline;
        ServersOnlineOMP = serversOnlineOMP;
    }
}

public class LanguageStats
{
    public int Spanish { get; set; }
    public int Russian { get; set; }
    public int English { get; set; }
    public int Romanian { get; set; }
    public int Portuguese { get; set; }
    public int Asia { get; set; }
    public int EastEuro { get; set; }
    public int WestEuro { get; set; }
    public int Other { get; set; }

    public LanguageStats()
    {
        Spanish = 0;
        Russian = 0;
        English = 0;
        Romanian = 0;
        Portuguese = 0;
        Asia = 0;
        EastEuro = 0;
        WestEuro = 0;
        Other = 0;
    }
}

public class GamemodeStats
{
    public int Deathmatch { get; set; }
    public int Roleplay { get; set; }
    public int RaceStunt { get; set; }
    public int CNR { get; set; }
    public int FreeRoam { get; set; }
    public int Survival { get; set; }
    public int VehSim { get; set; }
    public int Other { get; set; }

    public GamemodeStats()
    {
        Deathmatch = 0;
        Roleplay = 0;
        RaceStunt = 0;
        CNR = 0;
        FreeRoam = 0;
        Survival = 0;
        VehSim = 0;
        Other = 0;
    }
}