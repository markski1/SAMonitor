using SAMPQuery;

namespace SAMonitor.Data;

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
    }
}