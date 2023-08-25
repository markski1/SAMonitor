using Dapper;
using MySqlConnector;
using SAMPQuery;
using System.Timers;
using System.Xml.Linq;

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

            currentServers = servers.Where(x => x.LastUpdated > DateTime.Now - TimeSpan.FromHours(24)).ToList();

            UpdateMasterlist();

            UpdateBlacklist();

            CreateTimer();
        }

        public static async Task<string> AddServer(string ipAddr)
        {
            if (IsBlacklisted(ipAddr))
            {
                return "IP Address is blacklisted.";
            }

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
            CurrentCheckTimer.Interval = 2000000;
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

        private static async void UpdateBlacklist()
        {
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"SELECT * FROM blacklist";

            blacklist = (await conn.QueryAsync<string>(sql)).ToList();

            // check all servers for blacklists.
            foreach (var server in currentServers)
            {
                foreach (var blocked_addr in blacklist)
                {
                    if (server.IpAddr.Contains(blocked_addr))
                    {
                        sql = @"DELETE FROM blacklist WHERE ip_addr = @IpAddr";
                        await conn.ExecuteAsync(sql, new { server.IpAddr });
                        currentServers.Remove(server);
                    }
                }
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
        public string Version { get; set; }
        public string Language { get; set; }
        public string SampCac { get; set; }
        private List<Player> Players { get; set; }
        public int Sponsor { get; set; }

        // Database fetch constructor
        public Server(string ip_addr, string name, DateTime last_updated, int allows_dl, int lag_comp, string map_name, string gamemode, int players_online, int max_players, string website, string version, string language, string sampcac, int sponsor)
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
                serverInfo = await server.GetServerInfoAsync();
                serverRules = await server.GetServerRulesAsync();

                if (serverInfo is null || serverRules is null || serverInfo.HostName is null)
                {
                    Console.WriteLine($"Failed to query {IpAddr}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying {IpAddr}: {ex}");
                return false;
            }

            Name = serverInfo.HostName;
            PlayersOnline = serverInfo.Players;
            MaxPlayers = serverInfo.MaxPlayers;
            GameMode = serverInfo.GameMode;
            Language = serverInfo.Language ?? "Unknown";
            Version = serverRules.Version ?? "Unknown"; ;
            MapName = serverRules.MapName ?? "Unknown";
            SampCac = serverRules.SAMPCAC_Version ?? "Not required";
            LagComp = serverRules.Lagcomp;
            Website = serverRules.Weburl.ToString() ?? "Unknown";
            WorldTime = serverRules.WorldTime;
            LastUpdated = DateTime.Now;

            // This is not a standard latin 'c', this is cyrillic character 'с', which SA-MP servers commonly return in place of the spanish ñ, for some reason.
            // So, if the server doesn't seem russian, we replace that character for a proper ñ.
            // It's dirty but I can't think of a less disruptive way to address this issue.
            if (Language.ToLower().Contains("ru") == false)
            {
                Name = Name.Replace('с', 'ñ');
                Language = Language.Replace('с', 'ñ');
            }

            bool success = true;

            if (doUpdate)
            {
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