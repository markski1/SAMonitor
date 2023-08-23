using Microsoft.AspNetCore.Mvc;
using SAMonitor.Data;

namespace SAMonitor.Controllers
{
    [ApiController]
    [Route("api")]
    public class ServerController : ControllerBase
    {
        private static readonly string[] BoolValues = new[]
        {
            "Disabled", "Enabled"
        };

        private readonly ILogger<ServerController> _logger;

        public ServerController(ILogger<ServerController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetServer")]
        public dynamic GetServer(string ipAddr)
        {
            var result = ServerManager.ServerByIP(ipAddr);

            if (result is null)
                return new ErrorMessage("Server not found.");

            return result;
        }

        [HttpGet("GetAllServers")]
        public IEnumerable<Server> GetAllServers()
        {
            return ServerManager.servers;
        }

        [HttpGet("GetServerPlayers")]
        public dynamic GetServerPlayers(string ipAddr)
        {
            var result = ServerManager.ServerByIP(ipAddr);

            if (result is null)
                return new ErrorMessage("Server not found.");

            return result.Players;
        }

        [HttpGet("GetTotalPlayers")]
        public int GetTotalPlayers()
        {
            return ServerManager.servers.Sum(x => x.PlayersOnline);
        }

        [HttpGet("GetAmountServers")]
        public int GetAmountServers()
        {
            return ServerManager.servers.Count;
        }

        [HttpGet("GetMasterlist")]
        public string GetMasterlist()
        {
            string list = "";
            foreach (var server in ServerManager.servers)
            {
                list += $"{server.IpAddr}\n";
            }
            return list;
        }
    }
}