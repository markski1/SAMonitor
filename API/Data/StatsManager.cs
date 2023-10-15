using Dapper;
using MySqlConnector;
using SAMonitor.Utils;
using System.Timers;

namespace SAMonitor.Data;

public static class StatsManager
{
    private static readonly System.Timers.Timer ThreeMinuteTimer = new();

    // Public cached data references
    public static GlobalStats GlobalStats { get; private set; } = new(0, 0, 0, 0);
    public static GamemodeStats GamemodeStats { get; private set; } = new();
    public static LanguageStats LanguageStats { get; private set; } = new();
    public static List<GlobalMetrics> GlobalMetrics { get; private set; } = new();


    // Initializers

    public static void LoadStats()
    {
        List<Server> servers = new(ServerManager.GetServers());
        
        UpdateGlobalStats();
        UpdateGlobalMetrics();
        UpdateGamemodeStats(servers);
        UpdateLanguageStats(servers);

        CreateTimers();
    }

    private static void CreateTimers()
    {
        ThreeMinuteTimer.Elapsed += EveryFiveMinutes;
        ThreeMinuteTimer.AutoReset = true;
        ThreeMinuteTimer.Interval = 300000;
        ThreeMinuteTimer.Enabled = true;
    }

    private static void EveryFiveMinutes(object? sender, ElapsedEventArgs e)
    {
        _ = Task.Run(() =>
        {
            List<Server> servers = new(ServerManager.GetServers());

            UpdateGlobalStats();
            UpdateGlobalMetrics();
            UpdateGamemodeStats(servers);
            UpdateLanguageStats(servers);
        });
    }

    // Getters

    public static List<GlobalMetrics> GetGlobalMetrics(int hours)
    {
        DateTime RequestTime = DateTime.Now - TimeSpan.FromHours(hours);

        return GlobalMetrics.Where(x => x.Time > RequestTime).ToList();
    }

    // Statistics update functions

    private static void UpdateGlobalStats()
    {
        GlobalStats = new GlobalStats(
                serversOnline: ServerManager.ServerCount(),
                serversTracked: ServerManager.ServerCount(includeDead: true),
                serversOnlineOMP: ServerManager.ServerCount(onlyOMP: true),
                playersOnline: ServerManager.TotalPlayers()
            );
    }

    private static async void UpdateGlobalMetrics()
    {
        DateTime RequestTime = DateTime.Now - TimeSpan.FromHours(168);

        var conn = new MySqlConnection(MySQL.ConnectionString);
        var sql = @"SELECT players, servers, api_hits, time FROM metrics_global WHERE time > @RequestTime ORDER BY time DESC";

        GlobalMetrics = (await conn.QueryAsync<GlobalMetrics>(sql, new { RequestTime })).ToList();
    }

    private static void UpdateLanguageStats(IEnumerable<Server> servers)
    {
        LanguageStats = new();
        foreach (var server in servers)
        {
            string lang = server.Language.ToLower();

            if (lang.Contains("ru") || lang.Contains("ру"))
            {
                LanguageStats.Russian++;
                continue;
            }

            if (lang.Contains("esp") || lang.Contains("spa"))
            {
                LanguageStats.Spanish++;
                continue;
            }

            if (lang.Contains("ro"))
            {
                LanguageStats.Romanian++;
                continue;
            }

            if (lang.Contains("br") || lang.Contains("port") || lang.Contains("pt"))
            {
                LanguageStats.Portuguese++;
                continue;
            }

            if (lang.Contains("geor") || lang.Contains("balkan") || lang.Contains("ex-yu") || lang.Contains("shqip") || lang.Contains("bulg") || lang.Contains("srb") || lang.Contains("tur") || lang.Contains("ukr"))
            {
                LanguageStats.EastEuro++;
                continue;
            }

            if (lang.Contains("ger") || lang.Contains("pol") || lang.Contains("hung") || lang.Contains("mag") || lang.Contains("fr") || lang.Contains("belg") || lang.Contains("slov") || lang.Contains("lat") || lang.Contains("liet") || lang.Contains("it"))
            {
                LanguageStats.WestEuro++;
                continue;
            }

            if (lang.Contains("viet") || lang.Contains("tamil") || lang.Contains("ko") || lang.Contains("th") || lang.Contains("ch") || lang.Contains("bahasa") || lang.Contains("malay") || lang.Contains("indo") || lang.Contains("jp") || lang.Contains("jap"))
            {
                LanguageStats.Asia++;
                continue;
            }

            if (lang.Contains("en"))
            {
                LanguageStats.English++;
                continue;
            }

            LanguageStats.Other++;
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
                GamemodeStats.CNR++;
                continue;
            }

            if (gm.Contains("dm") || gm.Contains("dea") || gm.Contains("pvp") || gm.Contains("war") || name.Contains("war") || name.Contains("war"))
            {
                GamemodeStats.Deathmatch++;
                continue;
            }

            if (gm.Contains("rp") || gm.Contains("role") || gm.Contains("real") || name.Contains("role") || name.Contains(" rp"))
            {
                GamemodeStats.Roleplay++;
                continue;
            }

            if (gm.Contains("rac") || gm.Contains("stunt") || gm.Contains("drift") || name.Contains("race") || name.Contains("stunt") || name.Contains("drift"))
            {
                GamemodeStats.RaceStunt++;
                continue;
            }

            if (gm.Contains("surv") || name.Contains("surv") || gm.Contains("dayz") || name.Contains("dayz") || gm.Contains("zomb") || name.Contains("zomb") || name.Contains("stalk"))
            {
                GamemodeStats.Survival++;
                continue;
            }

            if (gm.Contains("pilot") || name.Contains("truck") || gm.Contains("pilot") || name.Contains("pilot") || gm.Contains("sim") || name.Contains("sim"))
            {
                GamemodeStats.VehSim++;
                continue;
            }

            if (gm.Contains("free") || name.Contains("freeroam"))
            {
                GamemodeStats.FreeRoam++;
                continue;
            }

            GamemodeStats.Other++;
        }
    }
}