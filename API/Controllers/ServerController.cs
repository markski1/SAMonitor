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

        [HttpGet("GetAllServers")]
        public IEnumerable<Server> GetAllServers()
        {
            return ServerManager.GetServers();
        }

        [HttpGet("GetFilteredServers")]
        public List<Server> GetFilteredServers(int show_empty = 0, string order = "none", string name = "unspecified", string gamemode = "unspecified", int paging_size = 0, int page = 0)
        {
            var servers = ServerManager.GetServers();

            // unless specified, don't show empty servers.
            if (show_empty == 0)
            {
                servers = servers.Where(x => x.PlayersOnline > 0);
            }

            if (name != "unspecified")
            {
                servers = servers.Where(x => x.Name.ToLower().Contains(name.ToLower()));
            }

            if (gamemode != "unspecified")
            {
                servers = servers.Where(x => x.GameMode.ToLower().Contains(gamemode.ToLower()));
            }

            // after ordering we exclusively manage lists
            // as lists guarantee order and generic enumerables do not.

            List<Server> orderedServers;

            // if specified, order
            if (order != "none")
            {
                // by player count
                if (order == "players")
                {
                    orderedServers = servers.OrderByDescending(x => x.PlayersOnline).ToList();
                }
                // by player count over max player ratio.
                else
                {
                    // show_empty=0 guarantees PlayersOnline will never be zero.
                    // otherwise we have to separate them
                    if (show_empty == 0)
                    {
                        orderedServers = servers.OrderBy(x => x.MaxPlayers / x.PlayersOnline).ToList();
                    }
                    else
                    {
                        var emptyServers = servers.Where(x => x.PlayersOnline == 0);
                        var populatedServers = servers.Where(x => x.PlayersOnline > 0);

                        orderedServers = populatedServers.OrderBy(x => x.MaxPlayers / x.PlayersOnline).ToList();
                        orderedServers.AddRange(emptyServers);
                    }
                }
            }
            else
            {
                orderedServers = servers.ToList();
            }

            // If we're paging, return a "page".
            if (paging_size > 0)
            {
                try
                {
                    return orderedServers.Skip(paging_size * page).Take(paging_size).ToList();
                }
                catch
                {
                    // nothing, just return the full list. this just protects from malicious pagingSize or page values.
                }
            }

            return orderedServers;
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
            return ServerManager.GetMasterlist();
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