using Dapper;
using MySqlConnector;
using SAMonitor.Utils;
using System.Timers;

namespace SAMonitor.Data;

public static class StatsManager
{
    private static readonly System.Timers.Timer ThreeMinuteTimer = new();

    // Public cached data references
    public static GlobalStats GlobalStats { get; private set; } = new(0, 0, 0, 0, 0);
    public static GamemodeStats GamemodeStats { get; private set; } = new();
    public static LanguageStats LanguageStats { get; private set; } = new();
    private static List<GlobalMetrics> GlobalMetrics { get; set; } = [];

    // Initializers

    public static void LoadStats()
    {
        UpdateStats();

        CreateTimers();
    }

    public static List<GlobalMetrics> GetGlobalMetrics(int hours)
    {
        DateTime requestTime = DateTime.UtcNow - TimeSpan.FromHours(hours);

        return GlobalMetrics.Where(x => x.Time > requestTime).ToList();
    }

    private static void CreateTimers()
    {
        ThreeMinuteTimer.Elapsed += UpdateStatsTimer;
        ThreeMinuteTimer.AutoReset = true;
        ThreeMinuteTimer.Interval = 300000;
        ThreeMinuteTimer.Enabled = true;
    }

    private static void UpdateStatsTimer(object? sender, ElapsedEventArgs e)
    {
        Thread statsThread = new(UpdateStats);
        statsThread.Start();
    }

    private static void UpdateStats()
    {
        List<Server> servers = new(ServerManager.GetServers());

        UpdateGlobalMetrics();
        UpdateGlobalStats(servers);
        UpdateGamemodeStats(servers);
        UpdateLanguageStats(servers);
    }

    // Statistics update functions

    private static void UpdateGlobalStats(List<Server> servers)
    {
        int allServers = ServerManager.GetAllServers().Count;

        int playerCount = servers.Sum(x => x.PlayersOnline);
        int onlineServers = servers.Count;
        int inhabitedServers = servers.Count(x => x.PlayersOnline > 0);
        int onlineServersOmp = servers.Count(x => x.IsOpenMp);

        GlobalStats = new GlobalStats(
                serversOnline: onlineServers,
                serversTracked: allServers,
                serversInhabited: inhabitedServers,
                serversOnlineOmp: onlineServersOmp,
                playersOnline: playerCount
            );
    }

    private static async void UpdateGlobalMetrics()
    {
        try
        {
            DateTime requestTime = DateTime.UtcNow - TimeSpan.FromDays(8);

            var conn = new MySqlConnection(MySql.ConnectionString);
            const string sql = "SELECT players, servers, omp_servers, time FROM metrics_global WHERE time > @RequestTime ORDER BY time DESC";

            GlobalMetrics = (await conn.QueryAsync<GlobalMetrics>(sql, new { RequestTime = requestTime })).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating global metrics. - {ex.Message}");
        }
    }

    private static void UpdateLanguageStats(IEnumerable<Server> servers)
    {
        LanguageStats = new();
        foreach (var server in servers)
        {
            string lang = server.Language.ToLower();

            if (lang.Contains("ru") || lang.Contains("ру"))
            {
                LanguageStats.Russian.Add(server);
                continue;
            }

            if (lang.Contains("esp") || lang.Contains("spa"))
            {
                LanguageStats.Spanish.Add(server);
                continue;
            }

            if (lang.Contains("ro"))
            {
                LanguageStats.Romanian.Add(server);
                continue;
            }

            if (lang.Contains("br") || lang.Contains("port") || lang.Contains("pt"))
            {
                LanguageStats.Portuguese.Add(server);
                continue;
            }

            if (lang.Contains("geor") || lang.Contains("balkan") || lang.Contains("ex-yu") || lang.Contains("shqip") || lang.Contains("bulg") || lang.Contains("srb") || lang.Contains("tur") || lang.Contains("ukr"))
            {
                LanguageStats.EastEuro.Add(server);
                continue;
            }

            if (lang.Contains("ger") || lang.Contains("pol") || lang.Contains("hung") || lang.Contains("mag") || lang.Contains("fr") || lang.Contains("belg") || lang.Contains("slov") || lang.Contains("lat") || lang.Contains("liet") || lang.Contains("it"))
            {
                LanguageStats.WestEuro.Add(server);
                continue;
            }

            if (lang.Contains("viet") || lang.Contains("tamil") || lang.Contains("ko") || lang.Contains("th") || lang.Contains("ch") || lang.Contains("bahasa") || lang.Contains("malay") || lang.Contains("indo") || lang.Contains("jp") || lang.Contains("jap"))
            {
                LanguageStats.Asia.Add(server);
                continue;
            }

            if (lang.Contains("en"))
            {
                LanguageStats.English.Add(server);
                continue;
            }

            LanguageStats.Other.Add(server);
        }
    }

    private static void UpdateGamemodeStats(IEnumerable<Server> servers)
    {
        GamemodeStats = new();

        foreach (var server in servers)
        {
            string gm = server.GameMode.ToLower();
            string name = server.Name.ToLower();

            if (gm.Contains("cnr") || gm.Contains("cop") || name.Contains("cnr"))
            {
                GamemodeStats.Cnr.Add(server);
                continue;
            }

            if (gm.Contains("dm") || gm.Contains("dea") || gm.Contains("pvp") || gm.Contains("war") || name.Contains("war") || name.Contains("war"))
            {
                GamemodeStats.Deathmatch.Add(server);
                continue;
            }

            if (gm.Contains("rp") || gm.Contains("role") || gm.Contains("real") || name.Contains("role") || name.Contains(" rp"))
            {
                GamemodeStats.Roleplay.Add(server);
                continue;
            }

            if (gm.Contains("rac") || gm.Contains("stunt") || gm.Contains("drift") || name.Contains("race") || name.Contains("stunt") || name.Contains("drift"))
            {
                GamemodeStats.RaceStunt.Add(server);
                continue;
            }

            if (gm.Contains("surv") || name.Contains("surv") || gm.Contains("dayz") || name.Contains("dayz") || gm.Contains("zomb") || name.Contains("zomb") || name.Contains("stalk"))
            {
                GamemodeStats.Survival.Add(server);
                continue;
            }

            if (gm.Contains("pilot") || name.Contains("truck") || gm.Contains("pilot") || name.Contains("pilot") || gm.Contains("sim") || name.Contains("sim"))
            {
                GamemodeStats.VehSim.Add(server);
                continue;
            }

            if (gm.Contains("free") || name.Contains("freeroam"))
            {
                GamemodeStats.FreeRoam.Add(server);
                continue;
            }

            GamemodeStats.Other.Add(server);
        }
    }
}