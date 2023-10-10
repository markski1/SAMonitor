using Dapper;
using System.Timers;
using MySqlConnector;
using SAMonitor.Utils;
using SAMonitor.Database;

namespace SAMonitor.Data;

public static class ServerManager
{
    private static List<Server> servers = new();

    private static List<Server> currentServers = new();

    private static List<string> blacklist = new();

    private static string MasterList_global = "";
    private static string MasterList_037 = "";
    private static string MasterList_03DL = "";

    public static readonly ServerRepository _interface = new();

    public static int ApiHits { get; set; } = 0;

    public static async Task<bool> LoadServers()
    {
        servers = await _interface.GetAllServersAsync();

        UpdateBlacklist();
        currentServers = servers.Where(x => x.LastUpdated > DateTime.Now - TimeSpan.FromHours(6)).ToList();
        UpdateMasterlist();

        CreateTimers();

        return true;
    }

    public static async Task<string> AddServer(string ipAddr)
    {
        if (IsBlacklisted(ipAddr)) return "IP Address is blacklisted.";
        if (servers.Any(x => x.IpAddr == ipAddr)) return "Server is already monitored.";

        var newServer = new Server(ipAddr);

        if (!await newServer.Query(false))
        {
            return "Server did not respond to query.";
        }

        if (newServer.Version.ToLower().Contains("crce"))
        {
            return "CR-MP servers are currently unsupported.";
        }

        // check for copies                                                                                     they usually try to get smart by slightly modifying the gamemode string;
        //                                                                                                      so a little flexibility on this one
        var copies = currentServers.Where(x => x.Name == newServer.Name && x.Language == newServer.Language && (x.GameMode == newServer.GameMode || x.Website == newServer.Website));

        var conn = new MySqlConnection(MySQL.ConnectionString);
        string sql;

        if (copies.Any())
        {
            return "Server is already monitored. Be advised: Sneaking in repeated IP's for the same server is a motive for blacklisting.";
        }
        else
        {
            // if there's an 'old' dead version of this, then delete it.
            copies = servers.Where(x => x.Name == newServer.Name && x.Language == newServer.Language && (x.GameMode == newServer.GameMode || x.Website == newServer.Website));

            foreach (var server in copies)
            {
                sql = "DELETE FROM servers WHERE id = @Id";
                await conn.QueryAsync(sql, new { server.Id });
            }

            servers.RemoveAll(x => copies.Contains(x));
        }

        if (await _interface.InsertServer(newServer))
        {
            newServer.Id = await _interface.GetServerID(ipAddr);
            servers.Add(newServer);
            currentServers.Add(newServer);

            return "Server added to SAMonitor.";
        }
        else
        {
            return "Sorry, there was an error adding your server to SAMonitor.";
        }
    }

    private static bool IsBlacklisted(string ipAddr)
    {
        foreach (var addr in blacklist)
        {
            if (ipAddr.Contains(addr))
                return true;
        }
        return false;
    }

    public static Server? ServerByIP(string ip)
    {
        var result = servers.Where(x => x.IpAddr.Contains(ip)).ToList();

        if (result.Count < 1)
        {
            return null;
        }

        if (result.Count > 0)
        {
            var newFind = result.Where(x => x.IpAddr.Contains("7777")).ToList();

            if (newFind.Count > 0)
            {
                return newFind.FirstOrDefault();
            }
        }

        return result.FirstOrDefault();
    }

    public static List<Server> ServersByName(string name)
    {
        var results = servers.Where(x => x.Name.Contains(name)).ToList();

        if (results is null)
        {
            return new List<Server>();
        }

        return results;
    }

    public static int TotalPlayers()
    {
        return currentServers.Sum(x => x.PlayersOnline);
    }

    public static int ServerCount(bool includeDead = false, bool onlyOMP = false)
    {
        List<Server> countServers;
        if (includeDead) countServers = servers;
        else countServers = currentServers;

        if (onlyOMP) return countServers.Where(x => x.IsOpenMp).Count();

        return countServers.Count;
    }

    public static List<Server> GetServers()
    {
        return currentServers;
    }

    public static string GetMasterlist(string version)
    {
        // global, 0.3.7 and 0.3DL masterlists are cached once every 30 minutes, as they are the only "current" versions.
        if (version == "any") return MasterList_global;
        else if (version.Contains("3.7")) return MasterList_037;
        else if (version.Contains("DL")) return MasterList_03DL;
        else
        {
            // failing that, generate whatever got requested... I guess!
            string newList = "";

            currentServers.ForEach(x => 
            { 
                if (x.Version.Contains(version)) newList += $"{x.IpAddr}\n";
            });

            return newList;
        }
    }

    public static int GetServerIDFromIP(string ip_addr)
    {
        try
        {
            var server = servers.Where(x => x.IpAddr.Contains(ip_addr)).Single();
            return server.Id;
        }
        catch
        {
            return -1;
        }
    }

    private static readonly System.Timers.Timer ThirtyMinuteTimer = new();

    private static void CreateTimers()
    {
        ThirtyMinuteTimer.Elapsed += EveryThirtyMinutes;
        ThirtyMinuteTimer.AutoReset = true;
        ThirtyMinuteTimer.Interval = 1800000;
        ThirtyMinuteTimer.Enabled = true;
    }

    private static void EveryThirtyMinutes(object? sender, ElapsedEventArgs e)
    {
        // Update the Blacklist.
        UpdateBlacklist();

        // Update the current servers with only the ones which have responded in the last 6 hours
        currentServers = servers.Where(x => x.LastUpdated > DateTime.Now - TimeSpan.FromHours(6)).ToList();

        // Update the Masterlist accordingly.
        UpdateMasterlist();

        // Last of all, save the metrics.
        SaveMetrics();
    }

    private static async void SaveMetrics()
    {
        // don't save metrics unless in production
        #if !DEBUG
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"INSERT INTO metrics_global (players, servers, api_hits) VALUES(@_players, @_servers, @ApiHits)";

            int _servers = currentServers.Count;

            int _players = TotalPlayers();

            await conn.ExecuteAsync(sql, new { _players, _servers, ApiHits });
        #else
            // just to make the compiler happy, do an await in debug mode
            await Task.Delay(1);
        #endif
        ApiHits = 0;
    }

    private static void UpdateMasterlist()
    {
        Random rng = new();
        // To keep things somewhat fair, shuffle the position of all servers

        int n = currentServers.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (currentServers[n], currentServers[k]) = (currentServers[k], currentServers[n]);
        }

        MasterList_global = "";
        MasterList_037 = "";
        MasterList_03DL = "";
        n = 0;
        foreach (var server in currentServers)
        {
            server.ShuffledOrder = n;
            n++;
            // passworded servers don't make it to the masterlist.
            if (server.RequiresPassword) continue;

            MasterList_global += $"{server.IpAddr}\n";
            if (server.Version.Contains("3.7"))
            {
                MasterList_037 += $"{server.IpAddr}\n";
            }
            else if (server.Version.Contains("DL"))
            {
                MasterList_03DL += $"{server.IpAddr}\n";
            }
        }
    }

    private static async void UpdateBlacklist()
    {
        var conn = new MySqlConnection(MySQL.ConnectionString);

        var sql = @"SELECT ip_addr FROM blacklist";

        blacklist = (await conn.QueryAsync<string>(sql)).ToList();

        foreach (var blocked_addr in blacklist)
        {
            sql = @"DELETE FROM servers WHERE ip_addr LIKE @BlockedAddr";
            await conn.ExecuteAsync(sql, new { BlockedAddr = $"%{blocked_addr}%" });
        }

        servers = servers.Where(x => !blacklist.Any(addr => x.IpAddr.Contains(addr))).ToList();
    }
}
