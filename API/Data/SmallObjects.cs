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