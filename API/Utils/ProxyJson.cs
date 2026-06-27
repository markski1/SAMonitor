using System.Text.Json;

namespace SAMonitor.Utils;

internal static class ProxyJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static ProxyQueryResponse? DeserializeQueryResponse(string json)
    {
        return JsonSerializer.Deserialize<ProxyQueryResponse>(json, Options);
    }

    internal static ProxyPlayersResponse? DeserializePlayersResponse(string json)
    {
        return JsonSerializer.Deserialize<ProxyPlayersResponse>(json, Options);
    }
}

internal sealed class ProxyQueryResponse
{
    public ProxyServerInfo? Info { get; init; }
    public ProxyServerRules? Rules { get; init; }
}

internal sealed class ProxyServerInfo
{
    public string? HostName { get; init; }
    public ushort? Players { get; init; }
    public ushort? MaxPlayers { get; init; }
    public string? GameMode { get; init; }
    public string? Language { get; init; }
    public bool? Password { get; init; }
}

internal sealed class ProxyServerRules
{
    public bool? LagComp { get; init; }
    public string? MapName { get; init; }
    public string? Version { get; init; }
    public string? SampcacVersion { get; init; }
    public int? Weather { get; init; }
    public string? WebUrl { get; init; }
    public string? WorldTime { get; init; }
}

internal sealed class ProxyPlayersResponse
{
    public List<ProxyServerPlayer>? Players { get; init; }
}

internal sealed class ProxyServerPlayer
{
    public byte? PlayerId { get; init; }
    public string? PlayerName { get; init; }
    public int? PlayerScore { get; init; }
    public int? PlayerPing { get; init; }
}
