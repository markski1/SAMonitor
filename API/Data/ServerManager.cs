using MySqlConnector;
using System.Timers;
using Dapper;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using System.Linq;

namespace SAMonitor.Data
{
    public static class ServerManager
    {
        private static List<Server> servers = new();

        private static List<Server> currentServers = new();

        private static List<string> blacklist = new();

        private static string MasterList = "";

        public static async void LoadServers()
        {
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"SELECT ip_addr, name, last_updated, allows_dl, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac, sponsor FROM servers";

            servers = (await conn.QueryAsync<Server>(sql)).ToList();

            UpdateBlacklist();

            CreateTimer();

            currentServers = servers.Where(x => x.LastUpdated > DateTime.Now - TimeSpan.FromHours(24)).ToList();
            UpdateMasterlist();
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

            // check for copies
            var copies = currentServers.Where(x => x.Name == newServer.Name && x.Language == newServer.Language && (x.GameMode == newServer.GameMode || x.Website == newServer.Website));

            if (copies.Any())
            {
                return "Server is already monitored. Be advised: Sneaking in repeated IP's for the same server is a motive for blacklisting.";
            }

            bool success;

            Console.WriteLine($"Added server {ipAddr}");

            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"INSERT INTO servers (ip_addr, name, last_updated, allows_dl, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac)
                        VALUES(@IpAddr, @Name, @LastUpdated, @AllowsDL, @LagComp, @MapName, @GameMode, @PlayersOnline, @MaxPlayers, @Website, @Version, @Language, @SampCac)";

            try
            {
                success = (await conn.ExecuteAsync(sql, new
                {
                    newServer.IpAddr,
                    newServer.Name,
                    newServer.LastUpdated,
                    newServer.AllowsDL,
                    newServer.LagComp,
                    newServer.MapName,
                    newServer.GameMode,
                    newServer.PlayersOnline,
                    newServer.MaxPlayers,
                    newServer.Website,
                    newServer.Version,
                    newServer.Language,
                    newServer.SampCac
                })) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add {ipAddr} to the database: {ex}");
                success = false;
            }

            if (success)
            {
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

        public static int ServerCount(int includeDead)
        {
            if (includeDead == 0) return currentServers.Count;
            else return servers.Count;
        }

        public static IEnumerable<Server> GetServers()
        {
            return currentServers;
        }

        public static string GetMasterlist()
        {
            return MasterList;
        }

        private static readonly System.Timers.Timer CurrentCheckTimer = new();

        private static void CreateTimer()
        {
            Random rand = new();
            CurrentCheckTimer.Elapsed += EveryThirtyMinutes;
            CurrentCheckTimer.AutoReset = true;
            CurrentCheckTimer.Interval = 1800000;
            CurrentCheckTimer.Enabled = true;
        }

        private static void EveryThirtyMinutes(object? sender, ElapsedEventArgs e)
        {
            // Update the Blacklist.
            UpdateBlacklist();

            // Update the current servers with only the ones which have responded in the last 24 hours.
            currentServers = servers.Where(x => x.LastUpdated > DateTime.Now - TimeSpan.FromHours(24)).ToList();

            // Update the Masterlist accordingly.
            UpdateMasterlist();

            // Last of all, save the metrics.
            SaveMetrics();
        }

        private static async void SaveMetrics()
        {
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"INSERT INTO metrics_global (players, servers) VALUES(@_players, @_servers)";

            int _servers = currentServers.Count;

            int _players = TotalPlayers();

            await conn.ExecuteAsync(sql, new { _players, _servers });
        }

        private static void UpdateMasterlist()
        {
            MasterList = "";
            foreach (var server in currentServers)
            {
                MasterList += $"{server.IpAddr}\n";
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
}
