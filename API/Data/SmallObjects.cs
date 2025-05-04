using System.Text.Json.Serialization;
using SAMonitor.Utils;

namespace SAMonitor.Data;

// Internal use classes.

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
     * I don't care that there's "prettier" ways to do this. I don't want this to be pretty, I want it to be performant.
     * 
     * Don't tell me about all the fancy ways this could be refactored to be "pretty", unless they run as fast as this does.
     * 
     * Also, it might seem counter-intuitive, but using ToList() after every filter is actually faster than just doing it once at the end.
     * If you don't believe me, look into how lambda expression trees work in LINQ. It's rather trippy.
     */
    public List<Server> GetFilteredServers()
    {
        List<Server> servers = new(ServerManager.GetServers());

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

        if (RequireSampCac)
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
            // by player count over the max player ratio.
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
            // if "none", then order by the ShuffleOrder, which gets shuffled every 30 minutes.
            servers = servers.OrderBy(x => x.ShuffledOrder).ToList();
        }

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