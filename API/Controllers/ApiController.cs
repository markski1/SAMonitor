using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SAMonitor.Data;
using SAMonitor.Database;
using SAMonitor.Utils;
// ReSharper disable InconsistentNaming

namespace SAMonitor.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
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
            // return the full list. this just protects from malicious pagingSize or page values.
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
    public List<GlobalMetrics> GetGlobalMetrics(int hours = 6, bool skip_trimming = false)
    {
        return StatsManager.GetGlobalMetrics(hours, skip_trimming);
    }

    [HttpGet("GetMasterlist")]
    public string GetMasterlist(string version = "any")
    {
        return ServerManager.GetMasterlist(version);
    }

    [HttpGet("GetEveryIP")]
    public string GetEveryIP()
    {
        return ServerManager.GetEveryIp();
    }

    [HttpGet("AddServer")]
    [EnableRateLimiting("fixed")]
    public async Task<string> AddServer([FromQuery] string ip_addr)
    {
        ip_addr = ip_addr.Trim();
        string validIP = await Helpers.ValidateIPv4(ip_addr);
        
        if (validIP != "invalid")
            return (await ServerManager.AddServer(validIP)).Message;
        
        return "Entered IP address or hostname is invalid or failing to resolve.";
    }

    [HttpPost("AddServer")]
    [Consumes("application/json")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType<AddServerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddServerPost([FromBody] AddServerRequest request)
    {
        try
        {
            string ipAddr = await Helpers.ValidateIPv4(request.IpAddr.Trim());
            if (ipAddr == "invalid")
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid address.", detail: "Enter a valid IP address or hostname.");

            return BuildAddServerResponse(ipAddr, await ServerManager.AddServer(ipAddr));
        }
        catch (Exception ex)
        {
            await Helpers.LogError("AddServer", ex);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Server submission failed.");
        }
    }

    internal IActionResult BuildAddServerResponse(string ipAddr, ServerSubmissionResult result)
    {
        if (result.Outcome == ServerSubmissionOutcome.Added)
            return CreatedAtAction(nameof(GetServerByIp), new { ip_addr = ipAddr }, new AddServerResponse(ipAddr, result.Message));

        int status = result.Outcome switch
        {
            ServerSubmissionOutcome.Blacklisted => StatusCodes.Status403Forbidden,
            ServerSubmissionOutcome.AlreadyMonitored or ServerSubmissionOutcome.Duplicate => StatusCodes.Status409Conflict,
            ServerSubmissionOutcome.RecentlyFailed or ServerSubmissionOutcome.Unresponsive or ServerSubmissionOutcome.Unsupported => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
        return Problem(statusCode: status, title: "Server submission failed.", detail: result.Message);
    }

    [HttpGet("GetServerMetrics")]
    public async Task<List<ServerMetrics>> GetServerMetrics(string ip_addr = "none", int hours = 6, int include_misses = 0)
    {
        DateTime RequestTime = DateTime.UtcNow - TimeSpan.FromHours(hours);

        int Id = ServerManager.GetServerIdFromIp(ip_addr);

        return await ServerRepository.GetServerMetrics(Id, RequestTime, include_misses);
    }
}
