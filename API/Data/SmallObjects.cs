using SAMPQuery;
using System.Xml.Linq;
using System;

namespace SAMonitor.Data;

// Internal use classes.

public class ServerFilterer
{
    public bool ShowEmpty { get; set; }
    public bool ShowPassworded { get; set; }
    public bool HideRoleplay { get; set; }
    public bool RequireSampCAC { get; set; }
    public string Name { get; set; }
    public string Gamemode { get; set; }
    public string Version { get; set; }
    public string Language { get; set; }
    public string Order { get; set; }

    public ServerFilterer(bool showEmpty, bool showPassworded, bool hideRoleplay, bool requireSampCAC, string name, string gamemode, string version, string language, string order)
    {
        ShowEmpty = showEmpty;
        ShowPassworded = showPassworded;
        HideRoleplay = hideRoleplay;
        RequireSampCAC = requireSampCAC;
        Name = name;
        Gamemode = gamemode;
        Version = version;
        Language = language;
        Order = order;
    }

    /*
     * People who hate if statements for no reason beware:
     * 
     * I don't care that there's "prettier" ways to do this. I don't want this to be pretty, I want it to be performant.
     * 
     * Don't tell me about all the fancy ways this could be refactored to be "pretty", unless they run as fast as this does.
     */
    public List<Server> GetFilteredServers()
    {
        var servers = ServerManager.GetServers();

        // unless specified, don't show empty servers.
        if (!ShowEmpty)
        {
            servers = servers.Where(x => x.PlayersOnline > 0);
        }

        if (!ShowPassworded)
        {
            servers = servers.Where(x => x.RequiresPassword == false);
        }

        if (!HideRoleplay)
        {
            // safe to assume the substring "rp" or "role" in the gamemode can mean nothing but a roleplay server.
            servers = servers.Where(x => !x.GameMode.ToLower().Contains("rp") && !x.GameMode.ToLower().Contains("role"));

            // when checking by the name however we must be conservative.
            servers = servers.Where(x => !x.Name.ToLower().Contains("roleplay") && !x.Name.ToLower().Contains("role play"));
        }

        if (!RequireSampCAC)
        {
            servers = servers.Where(x => !x.SampCac.ToLower().Contains("not required"));
        }

        if (Name != "unspecified")
        {
            servers = servers.Where(x => x.Name.ToLower().Contains(Name));
        }

        if (Version != "any")
        {
            servers = servers.Where(x => x.Version.ToLower().Contains(Version));
        }

        // In the future, should probably have a way to specify a language in a more broad sense rather than by string,
        // as server operators define languages in rather inconsistent ways.
        if (Language != "any")
        {
            servers = servers.Where(x => x.Language.ToLower().Contains(Language));
        }

        if (Gamemode != "unspecified")
        {
            servers = servers.Where(x => x.GameMode.ToLower().Contains(Gamemode));
        }

        List<Server> filteredServers = servers.ToList();

        // if specified, order
        if (Order != "none")
        {
            // by player count
            if (Order == "players")
            {
                filteredServers = servers.OrderByDescending(x => x.PlayersOnline).ToList();
            }
            // by player count over max player ratio.
            else
            {
                // show_empty=0 guarantees PlayersOnline will never be zero.
                // otherwise we have to separate them
                if (!ShowEmpty)
                {
                    filteredServers = servers.OrderBy(x => x.MaxPlayers / x.PlayersOnline).ToList();
                }
                else
                {
                    var emptyServers = servers.Where(x => x.PlayersOnline == 0);
                    var populatedServers = servers.Where(x => x.PlayersOnline > 0);

                    filteredServers = populatedServers.OrderByDescending(x => x.PlayersOnline / x.MaxPlayers).ToList();
                    filteredServers.AddRange(emptyServers);
                }
            }
        }
        else
        {
            // if "none", then order by the ShuffleOrder which gets shuffled every 30 minutes.
            filteredServers = servers.OrderBy(x => x.ShuffledOrder).ToList();
        }

        return filteredServers;
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

    public GlobalStats(int playersOnline, int serversTracked, int serversOnline)
    {
        this.PlayersOnline = playersOnline;
        this.ServersTracked = serversTracked;
        this.ServersOnline = serversOnline;
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