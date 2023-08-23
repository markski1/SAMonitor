using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Common;
using SAMonitor.Data;
using System.Net;

namespace SAMonitor.Controllers
{
    [ApiController]
    [Route("[controller]")]
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

        [HttpGet(Name = "GetServer")]
        public dynamic GetServers(string ipAddr)
        {
            var result = ServerManager.ServerByIP(ipAddr);

            if (result is null)
                return new ErrorMessage("Server not found.");

            return result;
        }

        [HttpGet(Name = "GetAllServers")]
        public dynamic Get()
        {
            return ServerManager.servers;
        }

        [HttpGet(Name = "GetServerPlayers")]
        public dynamic Get(string ipAddr)
        {
            var result = ServerManager.ServerByIP(ipAddr);

            if (result is null)
                return new ErrorMessage("Server not found.");

            return result.players;
        }

        [HttpGet(Name = "GetTotalPlayers")]
        public dynamic GetTotalPlayers()
        {
            return ServerManager.servers.Sum(x => x.playersOnline);
        }

        [HttpGet(Name = "GetServerCount")]
        public dynamic GetServerCount()
        {
            return ServerManager.servers.Count;
        }
    }
}