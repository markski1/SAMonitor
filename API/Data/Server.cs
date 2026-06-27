// ReSharper disable RedundantUsingDirective
// ReSharper disable InconsistentNaming
using SAMonitor.Utils;
using System.Timers;
using SAMonitor.Database;

namespace SAMonitor.Data;

public sealed class Server : IDisposable
{
    private static readonly HttpClient Client = new();
    public int Id { get; set; }
    public bool Success { get; init; }
    public DateTime LastUpdated { get; set; }
    public DateTime WorldTime { get; set; }
    public int PlayersOnline { get; set; }
    public int MaxPlayers { get; set; }
    public bool IsOpenMp { get; set; }
    public bool IsProxyQueried { get; set; }
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
    private readonly CancellationTokenSource _cts = new();
    private int _queryInterval = 1200000;

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
    }

    public void CreateTimer()
    {
        Random rand = new();
        // while initializing, do the first update at a random amount of time between 0 seconds and 20 minutes.
        // this is to avoid all servers doing the query at the same instant.
        _ = Task.Run(() => QueryLoop(rand.Next(1200000)));
    }

    private async Task QueryLoop(int initialDelay)
    {
        try
        {
            await Task.Delay(initialDelay, _cts.Token);
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Query();
                }
                catch (Exception ex)
                {
                    await Helpers.LogError($"QueryLoop {IpAddr}", ex);
                }
                await Task.Delay(_queryInterval, _cts.Token);
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            await Helpers.LogError($"Unhandled exception in QueryLoop for {IpAddr}", ex);
        }
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

        ServerInfo? serverInfo = null;
        ServerRules? serverRules = null;
        bool querySuccess = false;
        bool isProxy = false;

        try
        {
            serverInfo = await _query.GetServerInfoAsync();
            querySuccess = true;
            IsProxyQueried = false;
        }
        catch
        {
            // If we fail to query, then we defer to the secondary querying server. (Just to be sure it's not a fluke, or a OVH blockage.)
            if (QueryManagerProxy.ProxyUrl is not null)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    using var response = await Client.GetAsync($"{QueryManagerProxy.ProxyUrl}/query?ip={IpAddr}", cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = ProxyJson.DeserializeQueryResponse(json);
                        if (data?.Info is not null)
                        {
                            serverInfo = SqHelpers.NormalizeServerInfo(new ServerInfo
                            {
                                HostName = data.Info.HostName ?? "Unknown",
                                Players = data.Info.Players ?? 0,
                                MaxPlayers = data.Info.MaxPlayers ?? 0,
                                GameMode = data.Info.GameMode ?? "Unknown",
                                Language = data.Info.Language ?? "Unknown",
                                Password = data.Info.Password ?? false
                            });

                            if (data.Rules is not null)
                            {
                                serverRules = SqHelpers.NormalizeServerRules(new ServerRules
                                {
                                    Version = data.Rules.Version ?? "Unknown",
                                    MapName = data.Rules.MapName ?? "Unknown",
                                    SampcacVersion = data.Rules.SampcacVersion ?? "Unknown",
                                    LagComp = data.Rules.LagComp ?? false,
                                    WebUrl = string.IsNullOrWhiteSpace(data.Rules.WebUrl) || data.Rules.WebUrl == "Unknown" ? null : new Uri(data.Rules.WebUrl),
                                    WorldTime = SqHelpers.ParseTime(data.Rules.WorldTime ?? "00:00"),
                                    Weather = data.Rules.Weather ?? -1
                                }, serverInfo.Language);
                            }

                            isProxy = true;
                            IsProxyQueried = true;
                            querySuccess = true;
                        }
                    }
                }
                catch
                {
                    // Fallback failed too, continue to error handling
                }
            }
        }

        // If direct 'i' succeeded, kick off 'r' in parallel and gather both.
        Task<ServerRules>? rulesTask = null;
        if (querySuccess && !isProxy)
        {
            rulesTask = _query.GetServerRulesAsync();
        }

        if (!querySuccess)
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
                    _queryInterval = 86400000;
                }
                else if (downtime > TimeSpan.FromHours(3)) { // if the server's been dead for over 3 hours, only query every hour
                    _queryInterval = 3600000;
                }
                else // otherwise, always query every 20 minutes
                {
                    _queryInterval = 1200000;
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

        _queryInterval = 1200000;

        Name = serverInfo!.HostName;
        PlayersOnline = serverInfo.Players;
        MaxPlayers = serverInfo.MaxPlayers;
        GameMode = serverInfo.GameMode;
        Language = serverInfo.Language;
        RequiresPassword = serverInfo.Password;
        LastUpdated = DateTime.UtcNow;

        if (PlayersOnline > MaxPlayers) return false;

        if (serverRules is not null)
        {
            Version = serverRules.Version ?? "Unknown";
            MapName = serverRules.MapName ?? "Unknown";
            SampCac = serverRules.SampcacVersion ?? "Not required";
            LagComp = serverRules.LagComp;
            Website = serverRules.WebUrl is null ? "Unknown" : serverRules.WebUrl.ToString();
            WorldTime = serverRules.WorldTime;
            Weather = serverRules.Weather;
        }
        else if (isProxy)
        {
            // If we used a proxy, but it didn't return rules, we skip direct rules query to avoid timeouts.
            Version = "Unknown";
            MapName = "Unknown";
            SampCac = "Not required";
            LagComp = false;
            Website = "Unknown";
            WorldTime = DateTime.MinValue;
            Weather = -1;
        }
        else
        {
            // Await the in-flight 'r' query. If it failed, retry after a short delay to respect rate limits.
            try
            {
                serverRules = SqHelpers.NormalizeServerRules(await rulesTask!, serverInfo.Language);
            }
            catch
            {
                // The first attempt was rate-limited or timed out. Back off briefly, then try once more.
                await Task.Delay(500);
                try
                {
                    serverRules = SqHelpers.NormalizeServerRules(await _query.GetServerRulesAsync(), serverInfo.Language);
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("SocketException") == false) // I don't care to log network exceptions
                    {
                        Console.WriteLine($"Error getting rules for {IpAddr} : {ex}");
                    }
                }
            }

            if (serverRules is not null)
            {
                Version = serverRules.Version ?? "Unknown";
                MapName = serverRules.MapName ?? "Unknown";
                SampCac = serverRules.SampcacVersion ?? "Not required";
                LagComp = serverRules.LagComp;
                Website = serverRules.WebUrl is null ? "Unknown" : serverRules.WebUrl.ToString();
                WorldTime = serverRules.WorldTime;
                Weather = serverRules.Weather;
            }
        }

        // The version string returned by the server is a reliable enough signal for whether it is open.mp.
        // This used to be backed by a dedicated 'o' query, but that was wasteful and is no longer needed.
        IsOpenMp = Version.Contains("omp", StringComparison.OrdinalIgnoreCase);

        if (doUpdate)
        {
            ServerManager.MarkFilterCachesDirty();
            ServerUpdater.Queue(this);
        }

        return true;
    }

    private List<Player> _playerListCache = [];
    private DateTime _playerListTime = DateTime.MinValue;

    public async Task<List<Player>> GetPlayers()
    {
        if (_query is null) return [];

        if (DateTime.UtcNow - _playerListTime <= TimeSpan.FromMinutes(3)) return _playerListCache;

        try
        {
            List<ServerPlayer> serverPlayers;

            if (IsProxyQueried && QueryManagerProxy.ProxyUrl is not null)
            {
                serverPlayers = await GetPlayersFromProxyAsync();
            }
            else
            {
                serverPlayers = await _query.GetServerPlayersAsync(IsOpenMp);
            }

            // we pass it as a different type of object for API compatibility reasons.
            var players = serverPlayers.Select(player => new Player(player)).ToList();
            _playerListCache = players;
            _playerListTime = DateTime.UtcNow;
        }
        catch
        {
            // nothing to handle, this happens, usually, if the server has >100 players and is a SA-MP issue.
        }

        return _playerListCache;
    }

    private async Task<List<ServerPlayer>> GetPlayersFromProxyAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var response = await Client.GetAsync($"{QueryManagerProxy.ProxyUrl}/query/players?ip={IpAddr}", cts.Token);

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync();
            var data = ProxyJson.DeserializePlayersResponse(json);

            if (data?.Players is null)
                return [];

            return data.Players.Select(player => new ServerPlayer
            {
                PlayerId = player.PlayerId ?? 0,
                PlayerName = player.PlayerName ?? "Unknown",
                PlayerScore = player.PlayerScore ?? 0,
                PlayerPing = player.PlayerPing ?? 0
            }).ToList();
        }
        catch
        {
            return [];
        }
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

        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    ~Server()
    {
        Dispose(false);
    }
}
