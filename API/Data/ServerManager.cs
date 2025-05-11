﻿using Dapper;
using MySqlConnector;
using SAMonitor.Database;
using SAMonitor.Utils;
using System.Timers;

namespace SAMonitor.Data;

public static class ServerManager
{
    private static List<Server> _servers = [];

    private static List<Server> _currentServers = [];

    private static List<string> _blacklist = [];

    private static readonly List<string> FailedAddresses = [];

    private static string _masterListGlobal = "";
    private static string _masterList037 = "";
    private static string _masterList03Dl = "";

    public static async Task LoadServers()
    {
        _servers = await ServerRepository.GetAllServersAsync();

        UpdateBlacklist();
        _currentServers = _servers.Where(x => x.LastUpdated > DateTime.UtcNow - TimeSpan.FromHours(6)).ToList();
        _currentServers = _currentServers.Where(x => x.Name.Length > 0).ToList();
        UpdateMasterlist();

        CreateTimers();
    }

    public static async Task<string> AddServer(string ipAddr)
    {
        if (IsBlacklisted(ipAddr)) return "IP Address is blacklisted.";
        if (_servers.Any(x => x.IpAddr.Contains(ipAddr))) return "Server is already monitored.";
        if (FailedAddresses.Contains(ipAddr)) return "This IP address failed last time it was queried. Please try again in an hour.";

        var newServer = new Server(ipAddr);

        if (!await newServer.Query(false))
        {
            newServer.Dispose();
            FailedAddresses.Add(ipAddr);
            return "Server did not respond to query.";
        }

        if (newServer.Version.Contains("cr", StringComparison.CurrentCultureIgnoreCase))
        {
            newServer.Dispose();
            FailedAddresses.Add(ipAddr);
            return "CR-MP servers are currently unsupported.";
        }
        
        // Extract to avoid 'disposed at outer scope' false(?) positive in JetBrains.
        string newName = newServer.Name;
        string newLang = newServer.Language;
        string newWebsite = newServer.Website;

        // check for copies
        var copies = _currentServers.Where(x => 
            x.Name.Equals(newName, StringComparison.CurrentCultureIgnoreCase) && 
            x.Language.Equals(newLang, StringComparison.CurrentCultureIgnoreCase) && 
            x.Website.Equals(newWebsite, StringComparison.CurrentCultureIgnoreCase));

        if (copies.Any())
        {
            newServer.Dispose();
            FailedAddresses.Add(ipAddr);
            return "Server is already monitored. Be advised: Sneaking in repeated IPs for the same server is a motive for blacklisting.";
        }

        // if there's an 'old' dead version of this, then delete it.
        copies = _servers.Where(x => 
            x.Name.Equals(newName, StringComparison.CurrentCultureIgnoreCase) && 
            x.Language.Equals(newLang, StringComparison.CurrentCultureIgnoreCase) && 
            x.Website.Equals(newWebsite, StringComparison.CurrentCultureIgnoreCase));

        var enumerable = copies as Server[] ?? copies.ToArray();
        foreach (var server in enumerable)
        {
            var conn = new MySqlConnection(MySql.ConnectionString);
            const string sql = "DELETE FROM servers WHERE id = @Id";
            await conn.QueryAsync(sql, new { server.Id });
        }

        _servers.RemoveAll(x => enumerable.Contains(x));

        if (!await ServerRepository.InsertServer(newServer))
        {
            newServer.Dispose();
            return "Sorry, there was an error adding your server to SAMonitor.";
        }
        
        newServer.Id = await ServerRepository.GetServerId(ipAddr);
        _servers.Add(newServer);
        _currentServers.Add(newServer);

        return "Server added to SAMonitor.";
    }

    private static bool IsBlacklisted(string ipAddr)
    {
        return _blacklist.Any(ipAddr.Contains);
    }

    public static Server? ServerByIp(string ip)
    {
        var result = _servers.Where(x => x.IpAddr.Contains(ip)).ToList();

        switch (result.Count)
        {
            case < 1:
                return null;
            case > 1:
            {
                var newFind = result.Where(x => x.IpAddr.Contains("7777")).ToList();

                if (newFind.Count > 0)
                {
                    return newFind.FirstOrDefault();
                }

                break;
            }
        }
        
        return result.FirstOrDefault();
    }

    public static List<Server> GetAllServers()
    {
        return _servers;
    }

    public static List<Server> GetServers()
    {
        return _currentServers;
    }

    public static string GetMasterlist(string version)
    {
        // global, 0.3.7 and 0.3DL masterlists are cached once every 30 minutes, as they are the only "current" versions.
        if (version == "any") return _masterListGlobal;
        if (version.Contains("3.7")) return _masterList037;
        if (version.Contains("DL")) return _masterList03Dl;

        // failing all of the above, generate whatever got requested... I guess!
        string newList = "";

        _currentServers.ForEach(x =>
        {
            if (x.Version.Contains(version)) newList += $"{x.IpAddr}\n";
        });

        return newList;
    }

    public static string GetEveryIp()
    {
        string list = "";

        _servers.ForEach(x =>
        {
            list += $"{x.IpAddr}\n";
        });

        return list;
    }

    public static int GetServerIdFromIp(string ipAddr)
    {
        var server = ServerByIp(ipAddr);
        if (server is not null)
            return server.Id;

        return -1;
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
        Thread timedActions = new(TimedActions);
        timedActions.Start();
    }

    private static void TimedActions()
    {
        // Update the Blacklist.
        UpdateBlacklist();

        // Clean list of "recently attempted" IP addresses.
        FailedAddresses.Clear();

        // Update the current servers with only the ones which have responded in the last 12 hours
        _currentServers = _servers.Where(x => x.LastUpdated > DateTime.UtcNow - TimeSpan.FromHours(12)).ToList();

        _currentServers = _currentServers.Where(x => x.Name.Length > 0).ToList();

        // Update the Masterlist accordingly.
        UpdateMasterlist();

        // Last of all, save the metrics.
        SaveMetrics();
    }

    private static async void SaveMetrics()
    {
        // don't save metrics unless in production
        if (Helpers.IsDevelopment) return;
        
        using var getConn = DatabasePool.GetConnection();
        var conn = getConn.Db;

        const string sql = "INSERT INTO metrics_global (players, servers, omp_servers) VALUES(@_players, @_servers, @_omp_servers)";

        int servers = _currentServers.Count;

        int players = _currentServers.Sum(x => x.PlayersOnline);

        int ompServers = _currentServers.Count(x => x.IsOpenMp);

        await conn.ExecuteAsync(sql, new { _players = players, _servers = servers, _omp_servers = ompServers });
    }

    private static void UpdateMasterlist()
    {
        Random rng = new();
        // To keep things somewhat fair, shuffle the position of all servers every 30 minutes

        int n = _currentServers.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (_currentServers[n], _currentServers[k]) = (_currentServers[k], _currentServers[n]);
        }

        // Sponsor servers should stay at the top, however
        _currentServers.Sort((a, b) => b.Sponsor.CompareTo(a.Sponsor));

        _masterListGlobal = "";
        _masterList037 = "";
        _masterList03Dl = "";
        n = 0;
        foreach (var server in _currentServers)
        {
            server.ShuffledOrder = n;
            n++;
            // passworded servers don't make it to the masterlist.
            if (server.RequiresPassword) continue;

            _masterListGlobal += $"{server.IpAddr}\n";
            if (server.Version.Contains("3.7"))
            {
                _masterList037 += $"{server.IpAddr}\n";
            }
            else if (server.Version.Contains("DL"))
            {
                _masterList03Dl += $"{server.IpAddr}\n";
            }
        }
    }

    private static async void UpdateBlacklist()
    {
        using var getConn = DatabasePool.GetConnection();
        var conn = getConn.Db;

        var sql = "SELECT ip_addr FROM blacklist";

        _blacklist = (await conn.QueryAsync<string>(sql)).ToList();

        foreach (var blockedAddr in _blacklist)
        {
            sql = "DELETE FROM servers WHERE ip_addr LIKE @BlockedAddr";
            await conn.ExecuteAsync(sql, new { BlockedAddr = $"%{blockedAddr}%" });
        }

        _servers = _servers.Where(x => !_blacklist.Any(addr => x.IpAddr.Contains(addr))).ToList();
    }
}
