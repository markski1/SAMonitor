namespace SAMonitor.SampQuery.Types;

/// <summary>
/// Player Information
/// </summary>
public class ServerPlayer
{
    /// <summary>
    /// Player ID. Max value 255 (SA-MP feature (bug))
    /// </summary>
    public byte PlayerId { get; set; }

    /// <summary>
    /// Player Name. 
    /// </summary>
    public string PlayerName { get; set; } = "UNKNOWN";

    /// <summary>
    /// Player Score. 
    /// </summary>
    public int PlayerScore { get; set; }

    /// <summary>
    /// Ping of player. 
    /// </summary>
    public int PlayerPing { get; set; }
}