using Dapper;
using MySql.Data.MySqlClient;
using System;

namespace SAMonitor.Data
{
    public static class ServerManager
    {
        private static List<Server> servers = new();

        public static async void LoadServers()
        {
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"SELECT ip_addr, name, last_updated, allows_dl, lag_comp, map_name, gamemode, players_online, max_players, website FROM servers";

            servers = (await conn.QueryAsync<Server>(sql)).ToList();
        }

        public static async Task<bool> AddServer(string ipAddr)
        {
            var newServer = new Server(ipAddr);

            if (!await newServer.Query(false))
            {
                return false;
            }

            servers.Add(newServer);
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"INSERT INTO servers (ip_addr, name, last_updated, allows_dl, lag_comp, map_name, gamemode, players_online, max_players, website)
                            VALUES(@IpAddr, @Name, @LastUpdated, @AllowsDL, @LagComp, @MapName, @GameMode, @PlayersOnline, @MaxPlayers, @Website)";

            try
            {
                return (await conn.ExecuteAsync(sql, new { newServer.IpAddr, newServer.Name, newServer.LastUpdated, newServer.AllowsDL, newServer.LagComp, newServer.MapName, newServer.GameMode, newServer.PlayersOnline, newServer.MaxPlayers, newServer.Website })) > 0;
            }
            catch
            {
                return false;
            }
        }

        public static Server? ServerByIP(string ip)
        {
            var result = servers.Where(x => x.IpAddr.Contains(ip)).ToList();

            if (result.Count < 1) {
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
            return servers.Sum(x => x.PlayersOnline);
        }

        public static int ServerCount()
        {
            return servers.Count;
        }

        internal static IEnumerable<Server> GetServers()
        {
            return servers;
        }
    }

    public class Server
    {
        public bool Success { get; set; }
        public DateTime LastUpdated { get; set; }
        public int PlayersOnline { get; set; }
        public int MaxPlayers { get; set; }
        public bool AllowsDL { get; set; }
        public bool LagComp { get; set; }
        public string Name { get; set; }
        public string GameMode { get; set; }
        public string IpAddr { get; set; }
        public string MapName { get; set; }
        public string Website { get; set; }
        public List<Player> Players { get; set; }

        // Database fetch constructor
        public Server(string ip_addr, string name, DateTime last_updated, int allows_dl, int lag_comp, string map_name, string gamemode, int players_online, int max_players, string website)
        {
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
            Players = new();
            Success = true;
        }

        // Add constructor
        public Server(string ip_addr)
        {
            IpAddr = ip_addr;
            Name = "Unknown";
            LastUpdated = DateTime.Now;
            PlayersOnline = 0;
            MaxPlayers = 0;
            AllowsDL = false;
            LagComp = false;
            MapName = "Unknown";
            GameMode = "Unknown";
            Website = "Unknown";
            Players = new();
            Success = false;
        }

        public async Task<bool> Query(bool doUpdate = true)
        {
            // TODO: Server query logic

            if (doUpdate)
            {
                var conn = new MySqlConnection(MySQL.ConnectionString);

                var sql = @"UPDATE servers
                            SET ip_addr=@IpAddr, name=@Name, last_updated=@LastUpdated, allows_dl=@AllowsDL, lag_comp=@LagComp, map_name=@MapName, gamemode=@GameMode, players_online=@PlayersOnline, max_players=@MaxPlayers, website=@Website
                            WHERE ip_addr = @IpÁddr";

                try
                {
                    return (await conn.ExecuteAsync(sql, new { IpAddr, Name, LastUpdated, AllowsDL, LagComp, MapName, GameMode, PlayersOnline, MaxPlayers, Website })) > 0;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public Player(int id, string name, int score)
        {
            Id = id;
            Name = name;
            Score = score;
        }
    }
}