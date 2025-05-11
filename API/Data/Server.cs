// ReSharper disable RedundantUsingDirective
// ReSharper disable InconsistentNaming
using SAMonitor.Utils;
using System.Timers;
using SAMonitor.Database;

namespace SAMonitor.Data;

public sealed class Server : IDisposable
{
    public int Id { get; set; }
    public bool Success { get; init; }
    public DateTime LastUpdated { get; set; }
    public DateTime WorldTime { get; set; }
    public int PlayersOnline { get; set; }
    public int MaxPlayers { get; set; }
    public bool IsOpenMp { get; set; }
    public bool LagComp { get; set; }
    public string Name { get; set; }
    public string GameMode { get; set; }
    public string IpAddr { get; init; }
    public string MapName { get; set; }
    public string Website { get; set; }
    public string Version { get; set; }
    public string Language { get; set; }
    public string SampCac { get; set; }
    public bool RequiresPassword { get; set; }
    public int ShuffledOrder { get; set; }
    public int Weather { get; set; }
    public bool Sponsor { get; init; }

    private SampQuery? _query;
    private readonly System.Timers.Timer _queryTimer = new(); // 20 minute timer

    public Server(int id, string ip_addr, string name, DateTime last_updated, int is_open_mp, int lag_comp, string map_name, string gamemode, int players_online, int max_players, string website, string version, string language, string sampcac, DateTime sponsor_until, int weather)
    {
        Id = id;
        Name = name;
        LastUpdated = last_updated;
        PlayersOnline = players_online;
        MaxPlayers = max_players;
        IsOpenMp = is_open_mp == 1;
        LagComp = lag_comp == 1;
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
        Sponsor = sponsor_until > DateTime.UtcNow;

        CreateTimer();
        Weather = weather;
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
        Weather = -1;

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
        }
        catch
        {
            if (doUpdate)
            {
                if (!Helpers.IsDevelopment)
                {
                    // Server failed to respond. As such, in metrics, we store -1 players. Because having -1 players is not possible, this indicates downtime.
                    await ServerRepository.InsertServerMetrics(Id, -1);
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

            // Server objects are spawned on the first query. If a server was never successfully stored, then it'll die here.
            // Timer is killed, nothing else contains this object, and the garbage collector takes it from here.
            if (Id == -1)
            {
                Dispose();
            }

            return false;
        }

        _queryTimer.Interval = 1200000;

        Name = serverInfo.HostName;
        PlayersOnline = serverInfo.Players;
        MaxPlayers = serverInfo.MaxPlayers;
        GameMode = serverInfo.GameMode;
        Language = serverInfo.Language;
        RequiresPassword = serverInfo.Password;
        LastUpdated = DateTime.UtcNow;

        if (PlayersOnline > MaxPlayers) return false;

        await Task.Delay(500); // Await 500ms before next query to prevent ratelimit

        try
        {
            var serverRules = await _query.GetServerRulesAsync();

            Version = serverRules.Version ?? "Unknown";
            MapName = serverRules.MapName ?? "Unknown";
            SampCac = serverRules.SampcacVersion ?? "Not required";
            LagComp = serverRules.LagComp;
            Website = serverRules.WebUrl is null ? "Unknown" : serverRules.WebUrl.ToString();
            WorldTime = serverRules.WorldTime;
            Weather = serverRules.Weather;
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
        if (Language.Contains("ru", StringComparison.CurrentCultureIgnoreCase) == false && Language.Contains("ру", StringComparison.CurrentCultureIgnoreCase) == false)
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
            _ = Task.Run(() => IsOpenMp = _query.GetServerIsOmp());
        }
        
        if (doUpdate) ServerUpdater.Queue(this);

        return true;
    }

    private List<Player> _playerListCache = [];
    private DateTime _playerListTime = DateTime.MinValue;

    public async Task<List<Player>> GetPlayers()
    {
        List<Player> players = [];

        if (_query is null) return players;

        if (DateTime.UtcNow - _playerListTime <= TimeSpan.FromMinutes(3)) return _playerListCache;
        
        try
        {
            var serverPlayers = await _query.GetServerPlayersAsync();
            // we pass it as a different type of object for API compatibility reasons.
            _playerListCache.Clear();
            _playerListCache.AddRange(serverPlayers.Select(player => new Player(player)));
        }
        catch (Exception Ex)
        {
            Helpers.LogError($"get-players-{Id}", Ex);
            // nothing to handle, this happens, usually, if the server has >100 players and is a SA-MP issue.
        }

        _playerListCache = players;
        _playerListTime = DateTime.UtcNow;

        return _playerListCache;
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose managed resources, just the query timer.
        if (disposing) _queryTimer.Dispose();
    }

    ~Server()
    {
        Dispose(false);
    }
}