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

        [HttpGet("GetServerByIP")]
        public Server? GetServerByIP(string ip_addr)
        {
            var result = ServerManager.ServerByIP(ip_addr);

            return result;
        }

        [HttpGet("GetServersByName")]
        public List<Server>? GetServersByName(string name)
        {
            var result = ServerManager.ServersByName(name);

            return result;
        }

        [HttpGet("GetAllServers")]
        public IEnumerable<Server> GetAllServers()
        {
            return ServerManager.GetServers();
        }

        [HttpGet("GetServerPlayers")]
        public dynamic GetServerPlayers(string ip_addr)
        {
            var result = ServerManager.ServerByIP(ip_addr);

            if (result is null)
                return new Server(ip_addr);

            return result.Players;
        }

        [HttpGet("GetTotalPlayers")]
        public int GetTotalPlayers()
        {
            return ServerManager.TotalPlayers();
        }

        [HttpGet("GetAmountServers")]
        public int GetAmountServers()
        {
            return ServerManager.ServerCount();
        }

        [HttpGet("GetMasterlist")]
        public string GetMasterlist()
        {
            string list = "";
            foreach (var server in ServerManager.GetServers())
            {
                list += $"{server.IpAddr}\n";
            }
            return list;
        }

        [HttpGet("AddServer")]
        public async Task<bool> AddServer(string ip_addr)
        {
            return (await ServerManager.AddServer(ip_addr));
        }
    }
}