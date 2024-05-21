using Microsoft.AspNetCore.Mvc;
using SAMonitor.Data;
using SAMonitor.Database;
using SAMonitor.Utils;
// ReSharper disable InconsistentNaming

namespace SAMonitor.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private static readonly ServerRepository Interface = new();

    [HttpGet("CheckAlive")]
    public string CheckAlive()
    {
        return "SAMonitor lives!";
    }

    [HttpGet("GetServerByIP")]
    public Server? GetServerByIp(string ip_addr)
    {
        var result = ServerManager.ServerByIp(ip_addr);

        return result;
    }

    [HttpGet("GetAllServers")]
    public IEnumerable<Server> GetAllServers()
    {
        return ServerManager.GetServers();
    }

    [HttpGet("GetFilteredServers")]
    public List<Server> GetFilteredServers(int show_empty = 0, string order = "none", string name = "unspecified", string gamemode = "unspecified", int hide_roleplay = 0, int paging_size = 0, int page = 0, string version = "any", string language = "any", int require_sampcac = 0, int show_passworded = 0, int only_openmp = 0)
    {
        ServerFilterer filterServers = new(
            showEmpty: show_empty != 0,
            showPassworded: show_passworded != 0,
            hideRoleplay: hide_roleplay != 0,
            requireSampCac: require_sampcac != 0,
            onlyOpenMp: only_openmp != 0,
            order: order,
            name: name.ToLower(),
            gamemode: gamemode.ToLower(),
            language: language.ToLower(),
            version: version.ToLower()
        );

        List<Server> orderedServers = filterServers.GetFilteredServers();

        // If we're paging, return a "page".
        if (paging_size <= 0) return orderedServers;
        
        try
        {
            return orderedServers.Skip(paging_size * page).Take(paging_size).ToList();
        }
        catch
        {
            // nothing, just return the full list. this just protects from malicious pagingSize or page values.
        }

        return orderedServers;
    }

    [HttpGet("GetServerPlayers")]
    public async Task<List<Player>> GetServerPlayers(string ip_addr)
    {
        var result = ServerManager.ServerByIp(ip_addr);

        if (result is null) return [];

        return await result.GetPlayers();
    }

    [HttpGet("GetGlobalStats")]
    public GlobalStats GetGlobalStats()
    {
        return StatsManager.GlobalStats;
    }

    [HttpGet("GetLanguageStats")]
    public LanguageStats GetLanguageStats()
    {
        return StatsManager.LanguageStats;
    }

    [HttpGet("GetGamemodeStats")]
    public GamemodeStats GetGamemodeStats()
    {
        return StatsManager.GamemodeStats;
    }

    [HttpGet("GetGlobalMetrics")]
    public List<GlobalMetrics> GetGlobalMetrics(int hours = 6)
    {
        return StatsManager.GetGlobalMetrics(hours);
    }

    [HttpGet("GetMasterlist")]
    public string GetMasterlist(string version = "any")
    {
        return ServerManager.GetMasterlist(version);
    }

    [HttpGet("GetEveryIP")]
    public string GetEveryIP()
    {
        return ServerManager.GetEveryIP();
    }

    private long lastAddReq;

    [HttpGet("AddServer")]
    public async Task<string> AddServer(string ip_addr)
    {
        if (lastAddReq >= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return "Please try again in a few seconds.";
        }

        lastAddReq = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1;

        ip_addr = ip_addr.Trim();
        string validIP = Helpers.ValidateIPv4(ip_addr);
        if (validIP != "invalid")
            return (await ServerManager.AddServer(validIP));
        else
            return "Entered IP address or hostname is invalid or failing to resolve.";
    }

    [HttpGet("GetServerMetrics")]
    public async Task<List<ServerMetrics>> GetServerMetrics(string ip_addr = "none", int hours = 6, int include_misses = 0)
    {
        DateTime RequestTime = DateTime.Now - TimeSpan.FromHours(hours);

        int Id = ServerManager.GetServerIdFromIp(ip_addr);

        return await Interface.GetServerMetrics(Id, RequestTime, include_misses);
    }
}