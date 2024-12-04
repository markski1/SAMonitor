namespace SAMonitor.SampQuery.Types;

/// <summary>
/// Server Information
/// </summary>
public class ServerInfo
{
    /// <summary>
    /// Hostname
    /// </summary>
    public string HostName { get; set; } = "UNKNOWN";

    /// <summary>
    /// Gamemode text
    /// </summary>
    public string GameMode { get; set; } = "UNKNOWN";

    /// <summary>
    /// Server language 
    /// </summary>
    public string Language { get; set; } = "UNKNOWN";

    /// <summary>
    /// Number of players online
    /// </summary>
    public ushort Players { get; set; } = 0;

    /// <summary>
    /// Maximum number of players 
    /// </summary>
    public ushort MaxPlayers { get; set; } = 0;

    /// <summary>
    /// Password availability
    /// </summary>
    public bool Password { get; set; } = false;

    /// <summary>
    /// Ping of server
    /// </summary>
    public int ServerPing { get; set; } = -1;
}