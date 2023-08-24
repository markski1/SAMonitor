using Microsoft.AspNetCore.Mvc;
using SAMonitor.Data;
using System.ComponentModel;

namespace SAMonitor.Controllers
{
    [ApiController]
    [Route("api")]
    public class ServerController : ControllerBase
    {
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
        public async Task<List<Player>?> GetServerPlayers(string ip_addr)
        {
            var result = ServerManager.ServerByIP(ip_addr);

            if (result is null) return null;

            return await result.GetPlayers();
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

        [HttpPost("AddServerBulk")]
        public async Task<string> AddServerBulk(string ip_addrs)
        {
            string[] addrs = ip_addrs.Split(';');

            var servers = ServerManager.GetServers();

            int added = 0;
            int failed = 0;

            foreach (var addr in addrs)
            {
                if (servers.Any(x => x.IpAddr == addr)) continue;
                if (await ServerManager.AddServer(addr)) added++;
                else failed++;
            }

            return $"done. added {added} servers, {failed} could not be queried.";
        }
    }
}