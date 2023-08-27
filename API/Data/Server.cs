using SAMPQuery;
using Dapper;
using System.Timers;
using MySqlConnector;

namespace SAMonitor.Data;

public class Server
{
    private readonly System.Timers.Timer QueryTimer = new();
    public int Id { get; set; }
    public bool Success { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime WorldTime { get; set; }
    public int PlayersOnline { get; set; }
    public int MaxPlayers { get; set; }
    public bool AllowsDL { get; set; }
    public bool LagComp { get; set; }
    public string Name { get; set; }
    public string GameMode { get; set; }
    public string IpAddr { get; set; }
    public string MapName { get; set; }
    public string Website { get; set; }
    public string Version { get; set; }
    public string Language { get; set; }
    public string SampCac { get; set; }
    private List<Player> Players { get; set; }
    public int Sponsor { get; set; }

    // Database fetch constructor
    public Server(int id, string ip_addr, string name, DateTime last_updated, int allows_dl, int lag_comp, string map_name, string gamemode, int players_online, int max_players, string website, string version, string language, string sampcac, int sponsor)
    {
        Id = id;
        Name = name;
        LastUpdated = last_updated;
        PlayersOnline = players_online;
        MaxPlayers = max_players;
        AllowsDL = (allows_dl == 1);
        LagComp = (lag_comp == 1);
        MapName = map_name;
        GameMode = gamemode;
        IpAddr = ip_addr;
        Website = website;
        Version = version;
        SampCac = sampcac;
        Language = language;
        Players = new();
        WorldTime = DateTime.MinValue;
        Sponsor = sponsor;
        Success = true;

        CreateTimer();
    }

    // Add constructor
    public Server(string ip_addr)
    {
        Id = -1;
        IpAddr = ip_addr;
        Name = "Unknown";
        LastUpdated = DateTime.Now;
        PlayersOnline = 0;
        MaxPlayers = 0;
        AllowsDL = false;
        LagComp = false;
        Version = "Unknown";
        MapName = "Unknown";
        GameMode = "Unknown";
        Website = "Unknown";
        Language = "Unknown";
        SampCac = "Unknown";
        WorldTime = DateTime.MinValue;
        Players = new();
        Success = false;

        CreateTimer();
    }

    private void CreateTimer()
    {
        Random rand = new();
        QueryTimer.Elapsed += TimedQuery;
        QueryTimer.AutoReset = true;

        // while initializing, do the first update at a random amount of time between 0 seconds and 10 minutes.
        // this is to avoid all servers doing the query at the same instant.
        // after 1 call, it'll be set to the standard 15 minutes.
        QueryTimer.Interval = rand.Next(600000);
        QueryTimer.Enabled = true;
    }

    private void TimedQuery(object? sender, ElapsedEventArgs e)
    {
        QueryTimer.Interval = 1200000;
        _ = Query(true);
    }

    public async Task<bool> Query(bool doUpdate = true)
    {
        var server = new SampQuery(IpAddr);

        ServerInfo serverInfo;
        ServerRules serverRules;

        try
        {
            serverInfo = await server.GetServerInfoAsync();

            if (serverInfo is null || serverInfo.HostName is null)
            {
                Console.WriteLine($"Failed to query {IpAddr}");
                return false;
            }
        }
        catch
        {
            return false;
        }

        Name = serverInfo.HostName;
        PlayersOnline = serverInfo.Players;
        MaxPlayers = serverInfo.MaxPlayers;
        GameMode = serverInfo.GameMode;
        Language = serverInfo.Language ?? "Unknown";
        LastUpdated = DateTime.Now;

        try
        {
            serverRules = await server.GetServerRulesAsync();

            if (serverRules is not null)
            {
                Version = serverRules.Version ?? "Unknown";
                MapName = serverRules.MapName ?? "Unknown";
                SampCac = serverRules.SAMPCAC_Version ?? "Not required";
                LagComp = serverRules.Lagcomp;
                Website = serverRules.Weburl.ToString() ?? "Unknown";
                WorldTime = serverRules.WorldTime;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting rules for {IpAddr} : {ex}");
        }

        
        // This is not a standard latin 'c' being replaced, and it's not a standard latin 'py' being checked for.
        // This is cyrillic character 'с', which SA-MP servers commonly return in place of the spanish ñ, for some reason.
        // So, if the server doesn't seem russian, we replace that character for a proper ñ.
        // It's dirty, but I can't think of a less disruptive way to address this issue.
        // "ру" is also cyrillic "ru"
        if (Language.ToLower().Contains("ru") == false && Language.ToLower().Contains("ру") == false)
        {
            Name = Name.Replace('с', 'ñ');
            Language = Language.Replace('с', 'ñ');
            GameMode = GameMode.Replace('с', 'ñ');
            MapName = MapName.Replace('с', 'ñ');
        }

        bool success = true;

        if (doUpdate)
        {
            // first update the db entry for the server
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"UPDATE servers
                            SET ip_addr=@IpAddr, name=@Name, last_updated=@LastUpdated, allows_dl=@AllowsDL, lag_comp=@LagComp, map_name=@MapName, gamemode=@GameMode, players_online=@PlayersOnline, max_players=@MaxPlayers, website=@Website, version=@Version, language=@Language, sampcac=@SampCac
                            WHERE ip_addr = @IpAddr";

            try
            {
                success = (await conn.ExecuteAsync(sql, new { IpAddr, Name, LastUpdated, AllowsDL, LagComp, MapName, GameMode, PlayersOnline, MaxPlayers, Website, Version, Language, SampCac })) > 0;
            }
            catch
            {
                success = false;
            }

            // then add a metric entry. ONLY IF IN PRODUCTION.

            #if !DEBUG

                sql = @"INSERT INTO metrics_server (server_id, players) VALUES (@Id, @PlayersOnline)";

                await conn.ExecuteAsync(sql, new { Id, PlayersOnline });

            #endif
        }

        IEnumerable<ServerPlayer> serverPlayers;

        try
        {
            serverPlayers = server.GetServerPlayers();

            if (serverPlayers is not null)
            {
                Players.Clear();
                foreach (var player in serverPlayers) Players.Add(new Player(player));
            }
        }
        catch
        {
            // nothing to handle, this happens if server has >100 players and is a SA-MP issue.
        }

        return success;
    }

    public async Task<List<Player>> GetPlayers()
    {
        if (LastUpdated < (DateTime.Now - TimeSpan.FromMinutes(20)))
        {
            await Query();
        }

        return Players;
    }
}
