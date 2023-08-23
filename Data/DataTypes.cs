using System.Net;
using System.Runtime.CompilerServices;

namespace SAMonitor.Data
{
    public static class ServerManager
    {
        public static List<ServerInfo> servers = new();

        public static void LoadServers()
        {
            // TODO: Load the servers from the database.
        }

        public static ServerInfo? ServerByIP(string ip)
        {
            var result = servers.Where(x => x.ipAddr == ip).ToList().FirstOrDefault();

            if (result is null) {
                return null;
            }

            return result;
        }

        public static List<ServerInfo> ServersByName(string name)
        {
            var results = servers.Where(x => x.name.Contains(name)).ToList();

            if (results is null)
            {
                return new List<ServerInfo>();
            }

            return results;
        }
    }

    public class ServerInfo
    {
        public DateTime lastUpdated { get; set; }
        public int playersOnline { get; set; }
        public int maxPlayers { get; set; }
        public bool allowsDL { get; set; }
        public bool lagComp { get; set; }
        public string name { get; set; }
        public string gameMode { get; set; }
        public string ipAddr { get; set; }
        public string mapName { get; set; }
        public List<Player> players { get; set; }

        public ServerInfo(DateTime lastUpdated, int playersOnline, int maxPlayers, bool allowsDL, bool lagComp, string name, string gameMode, string ipAddr, string mapName, List<Player> players)
        {
            this.lastUpdated = lastUpdated;
            this.playersOnline = playersOnline;
            this.maxPlayers = maxPlayers;
            this.allowsDL = allowsDL;
            this.lagComp = lagComp;
            this.name = name;
            this.gameMode = gameMode;
            this.ipAddr = ipAddr;
            this.mapName = mapName;
            this.players = players;
        }
    }

    public class Player
    {
        public int id { get; set; }
        public string name { get; set; }
        public int score { get; set; }

        public Player(int id, string name, int score)
        {
            this.id = id;
            this.name = name;
            this.score = score;
        }
    }

    public class ErrorMessage
    {
        string Message { get; set; }

        public ErrorMessage(string message)
        {
            Message = message;
        }
    }
}