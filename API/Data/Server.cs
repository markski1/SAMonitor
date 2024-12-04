// ReSharper disable RedundantUsingDirective
// ReSharper disable InconsistentNaming
using SAMonitor.Utils;
using System.Timers;
using SAMonitor.Database;
using SAMonitor.SampQuery.Types;

namespace SAMonitor.Data;

public class Server : IDisposable
{
    private static readonly ServerRepository Interface = new();

    private readonly System.Timers.Timer _queryTimer = new(); // 20 minute timer
    public int Id { get; set; }
    public bool Success { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime WorldTime { get; set; }
    public int PlayersOnline { get; set; }
    public int MaxPlayers { get; set; }
    public bool IsOpenMp { get; set; }
    public bool LagComp { get; set; }
    public string Name { get; set; }
    public string GameMode { get; set; }
    public string IpAddr { get; set; }
    public string MapName { get; set; }
    public string Website { get; set; }
    public string Version { get; set; }
    public string Language { get; set; }
    public string SampCac { get; set; }
    public bool RequiresPassword { get; set; }
    public int ShuffledOrder { get; set; }
    public bool Sponsor { get; set; }

    private SampQuery.SampQuery? _query = null;

    public Server(int id, string ip_addr, string name, DateTime last_updated, int is_open_mp, int lag_comp, string map_name, string gamemode, int players_online, int max_players, string website, string version, string language, string sampcac, DateTime sponsor_until)
    {
        Id = id;
        Name = name;
        LastUpdated = last_updated;
        PlayersOnline = players_online;
        MaxPlayers = max_players;
        IsOpenMp = (is_open_mp == 1);
        LagComp = (lag_comp == 1);
        MapName = map_name;
        GameMode = gamemode;
        IpAddr = ip_addr;
        Website = website;
        Version = version;
        SampCac = sampcac;
        Language = language;
        WorldTime = DateTime.MinValue;
        Success = true;
        RequiresPassword = false;
        ShuffledOrder = 9999;
        Sponsor = (sponsor_until > DateTime.UtcNow);

        CreateTimer();
    }

    // Add constructor
    public Server(string ipAddr)
    {
        Id = -1;
        IpAddr = ipAddr;
        Name = "Unknown";
        LastUpdated = DateTime.UtcNow;
        PlayersOnline = 0;
        MaxPlayers = 0;
        IsOpenMp = false;
        LagComp = false;
        Version = "Unknown";
        MapName = "Unknown";
        GameMode = "Unknown";
        Website = "Unknown";
        Language = "Unknown";
        SampCac = "Unknown";
        WorldTime = DateTime.MinValue;
        Success = false;
        RequiresPassword = false;
        ShuffledOrder = 9999;
        Sponsor = false;

        CreateTimer();
    }

    private void CreateTimer()
    {
        Random rand = new();
        _queryTimer.Elapsed += TimedQuery;
        _queryTimer.AutoReset = true;

        // while initializing, do the first update at a random amount of time between 0 seconds and 20 minutes.
        // this is to avoid all servers doing the query at the same instant.
        // after 1 call, it'll be set to the standard 20 minutes.
        _queryTimer.Interval = rand.Next(1200000);
        _queryTimer.Enabled = true;
    }

    private void TimedQuery(object? sender, ElapsedEventArgs e)
    {
        _queryTimer.Interval = 1200000;
        Thread timedActions = new(TimedQueryLaunch);
        timedActions.Start();
    }

    public void TimedQueryLaunch()
    {
        _ = Query();
    }

    public async Task<bool> Query(bool doUpdate = true)
    {
        if (_query is null)
        {
            try
            {
                _query = new(IpAddr);
            }
            catch
            {
                return false;
            }
        }
        
        ServerInfo serverInfo;

        try
        {
            serverInfo = await _query.GetServerInfoAsync();

            if (serverInfo.HostName is null)
            {
                Console.WriteLine($"Server replied to query but response makes no sense: {IpAddr}");
                return false;
            }
        }
        catch
        {
            if (doUpdate)
            {
                if (!Helpers.IsDevelopment)
                {
                    // server failed to respond. As such, in metrics, we store -1 players. Because having -1 players is not possible, this indicates downtime.
                    await Interface.InsertServerMetrics(Id, -1);
                }

                TimeSpan downtime = DateTime.UtcNow - LastUpdated;

                if (downtime > TimeSpan.FromDays(7)) // if the server's been dead over a week, only query once a day.
                {
                    _queryTimer.Interval = 86400000;
                }
                else if (downtime > TimeSpan.FromDays(1)) // if the server's been dead over a day, only query every 3 hours.
                {
                    _queryTimer.Interval = 10800000;
                }
                else if (downtime > TimeSpan.FromHours(3)) { // if the server's been dead for over 3 hours, only query every hour
                    _queryTimer.Interval = 3600000;
                }
                else // otherwise, always query every 20 minutes
                {
                    _queryTimer.Interval = 1200000;
                }
            }

            // Server objects are spawned on first query. If a server was never succesfully stored, then it'll die here.
            // Timer is killed, nothing else contains this object, and the garbage collector takes it from here.
            if (Id == -1)
            {
                this.Dispose();
            }

            return false;
        }

        _queryTimer.Interval = 1200000;

        Name = serverInfo.HostName;
        PlayersOnline = serverInfo.Players;
        MaxPlayers = serverInfo.MaxPlayers;
        GameMode = serverInfo.GameMode ?? "Unknown";
        Language = serverInfo.Language ?? "Unknown";
        RequiresPassword = serverInfo.Password;
        LastUpdated = DateTime.UtcNow;

        if (PlayersOnline > MaxPlayers)
        {
            return false;
        }

        await Task.Delay(500); // Await 500ms before next query to prevent ratelimit

        try
        {
            var serverRules = await _query.GetServerRulesAsync();

            Version = serverRules.Version ?? "Unknown";
            MapName = serverRules.MapName ?? "Unknown";
            SampCac = serverRules.SAMPCAC_Version ?? "Not required";
            LagComp = serverRules.Lagcomp;
            Website = serverRules.Weburl is null ? "Unknown" : serverRules.Weburl.ToString();
            WorldTime = serverRules.WorldTime;
        }
        catch (Exception ex)
        {
            if (ex.ToString().Contains("SocketException") == false) // I don't care to log network exceptions
            {
                Console.WriteLine($"Error getting rules for {IpAddr} : {ex}");
            }
        }

        // SAMP encodes certain special latin characters as if they were Cyrillic.
        // So, if the server doesn't seem russian, we replace certain known ones.
        if (Language.ToLower().Contains("ru") == false && Language.ToLower().Contains("ру") == false)
        {
            Name = Helpers.BodgedEncodingFix(Name);
            Language = Helpers.BodgedEncodingFix(Language);
            GameMode = Helpers.BodgedEncodingFix(GameMode);
            MapName = Helpers.BodgedEncodingFix(MapName);
        }

        if (Version.Contains("omp"))
        {
            IsOpenMp = true;
        }
        else
        {
            await Task.Delay(500); // Await 500ms before next query to prevent ratelimit
            _ = Task.Run(() => IsOpenMp = _query.GetServerIsOMP());
        }
        
        if (doUpdate)
        {
            ServerUpdater.Queue(this);
        }

        return true;
    }

    private List<Player> _playerListCache = [];
    private DateTime _playerListTime = DateTime.MinValue;

    public async Task<List<Player>> GetPlayers()
    {
        List<Player> players = [];

        if (_query is null)
        {
            return players;
        }

        if (DateTime.UtcNow - _playerListTime > TimeSpan.FromMinutes(3))
        {
            try
            {
                var serverPlayersTask = _query.GetServerPlayersAsync();
                // Timeout at 2 seconds.
                if (await Task.WhenAny(serverPlayersTask, Task.Delay(2000)) == serverPlayersTask)
                {
                    await serverPlayersTask;
                    var serverPlayers = serverPlayersTask.Result;
                    // we pass it as a different type of object for API compatibility reasons.
                    _playerListCache.Clear();
                    _playerListCache.AddRange(serverPlayers.Select(player => new Player(player)));
                }
            }
            catch
            {
                // nothing to handle, this happens if server has >100 players and is a SA-MP issue.
            }

            _playerListCache = players;
            _playerListTime = DateTime.UtcNow;
        }
        
        return _playerListCache;
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _queryTimer.Dispose();
            }

            // Dispose unmanaged resources

            _disposed = true;
        }
    }

    ~Server()
    {
        Dispose(false);
    }
}
