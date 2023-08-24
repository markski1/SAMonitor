using Dapper;
using MySqlConnector;
using SAMPQuery;
using System.Timers;

namespace SAMonitor.Data
{
    public static class ServerManager
    {
        private static List<Server> servers = new();

        private static List<Server> currentServers = new();

        private static string MasterList = "";

        public static async void LoadServers()
        {
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"SELECT ip_addr, name, last_updated, allows_dl, lag_comp, map_name, gamemode, players_online, max_players, website, language, sampcac, sponsor FROM servers";

            servers = (await conn.QueryAsync<Server>(sql)).ToList();

            currentServers = servers.Where(x => x.LastUpdated > DateTime.Now - TimeSpan.FromHours(24)).ToList();

            UpdateMasterlist();

            CreateTimer();
        }

        public static async Task<bool> AddServer(string ipAddr)
        {
            var newServer = new Server(ipAddr);

            if (!await newServer.Query(false))
            {
                return false;
            }

            bool success;

            Console.WriteLine($"Added server {ipAddr}");

            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"INSERT INTO servers (ip_addr, name, last_updated, allows_dl, lag_comp, map_name, gamemode, players_online, max_players, website, language, sampcac)
                        VALUES(@IpAddr, @Name, @LastUpdated, @AllowsDL, @LagComp, @MapName, @GameMode, @PlayersOnline, @MaxPlayers, @Website, @Language, @SampCac)";

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
            }

            return success;
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
            return currentServers.Sum(x => x.PlayersOnline);
        }

        public static int ServerCount()
        {
            return currentServers.Count;
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
            CurrentCheckTimer.Elapsed += CurrentServersCheck;
            CurrentCheckTimer.AutoReset = true;
            CurrentCheckTimer.Interval = 3600000;
            CurrentCheckTimer.Enabled = true;
        }

        private static void CurrentServersCheck(object? sender, ElapsedEventArgs e)
        {
            currentServers = servers.Where(x => x.LastUpdated > DateTime.Now - TimeSpan.FromHours(24)).ToList();
            UpdateMasterlist();
        }

        private static void UpdateMasterlist()
        {
            MasterList = "";
            foreach (var server in currentServers)
            {
                MasterList += $"{server.IpAddr}\n";
            }
        }
    }

    public class Server
    {
        private readonly System.Timers.Timer QueryTimer = new();

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
        public string Language { get; set; }
        public string SampCac { get; set; }
        private List<Player> Players { get; set; }
        public int Sponsor { get; set; }

        // Database fetch constructor
        public Server(string ip_addr, string name, DateTime last_updated, int allows_dl, int lag_comp, string map_name, string gamemode, int players_online, int max_players, string website, string language, string sampcac, int sponsor)
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
            // Update every anywhere from 10 to 20 minutes
            // Just to avoid doing all requests at once.
            // Sponsor servers update every 5 minutes, always.
            if (Sponsor > 0) QueryTimer.Interval = 300000;
            else QueryTimer.Interval = 600000 + rand.Next(600000);
            QueryTimer.Enabled = true;
        }

        private void TimedQuery(object? sender, ElapsedEventArgs e)
        {
            _ = Query(true);
        }

        public async Task<bool> Query(bool doUpdate = true)
        {
            var server = new SampQuery(IpAddr);

            ServerInfo serverInfo;
            ServerRules serverRules;

            try
            {
                serverInfo = server.GetServerInfo();
                serverRules = server.GetServerRules();

                if (serverInfo is null || serverRules is null || serverInfo.HostName is null)
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
            MapName = serverRules.MapName ?? "Unknown";
            SampCac = serverRules.SAMPCAC_Version ?? "Not required";
            LagComp = serverRules.Lagcomp;
            Website = serverRules.Weburl.ToString() ?? "Unknown";
            WorldTime = serverRules.WorldTime;
            LastUpdated = DateTime.Now;

            bool success = true;

            if (doUpdate)
            {
                var conn = new MySqlConnection(MySQL.ConnectionString);

                var sql = @"UPDATE servers
                            SET ip_addr=@IpAddr, name=@Name, last_updated=@LastUpdated, allows_dl=@AllowsDL, lag_comp=@LagComp, map_name=@MapName, gamemode=@GameMode, players_online=@PlayersOnline, max_players=@MaxPlayers, website=@Website, language=@Language, sampcac=@SampCac
                            WHERE ip_addr = @IpAddr";

                try
                {
                    success = (await conn.ExecuteAsync(sql, new { IpAddr, Name, LastUpdated, AllowsDL, LagComp, MapName, GameMode, PlayersOnline, MaxPlayers, Website, Language, SampCac })) > 0;
                }
                catch
                {
                    success = false;
                }
            }

            IEnumerable<ServerPlayer> serverPlayers;

            try
            {
                serverPlayers = server.GetServerPlayers();

                if (serverPlayers is not null)
                {
                    Players.Clear();
                    foreach (var player in serverPlayers)
                    {
                        Players.Add(new Player(player));
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Failed to query players for {IpAddr}");
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
            Name = player.PlayerName;
            Score = player.PlayerScore;
        }
    }
}