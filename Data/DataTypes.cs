using Dapper;
using MySql.Data.MySqlClient;

namespace SAMonitor.Data
{
    public static class ServerManager
    {
        public static List<Server> servers = new();

        public static async void LoadServers()
        {
            var conn = new MySqlConnection(MySQL.ConnectionString);

            var sql = @"SELECT ip_addr, name, last_updated, allows_dl, lag_comp, map_name, gamemode, players_online, max_players, website FROM servers";

            servers = (await conn.QueryAsync<Server>(sql)).ToList();
        }

        public static void AddServer()
        {
            // TODO
        }

        public static Server? ServerByIP(string ip)
        {
            var result = servers.Where(x => x.IpAddr == ip).ToList().FirstOrDefault();

            if (result is null) {
                return null;
            }

            return result;
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
    }

    public class Server
    {
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

    public class ErrorMessage
    {
        public string Message { get; set; }

        public ErrorMessage(string message)
        {
            Message = message;
        }
    }
}