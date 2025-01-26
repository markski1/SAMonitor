namespace SAMonitor.SampQuery.Types;

/// <summary>
/// Server Rules
/// </summary>
public class ServerRules
{
    /// <summary>
    /// Lagcomp
    /// </summary>
    public bool Lagcomp { get; set; }

    /// <summary>
    /// Mapname
    /// </summary>
    public string? MapName { get; set; }

    /// <summary>
    /// Server version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// The version of Client Anti-Cheat, for SAMPCAC-enabled servers
    /// </summary>
    public string? SAMPCAC_Version { get; set; }

    /// <summary>
    /// ID of weather in server
    /// </summary>
    public int Weather { get; set; }

    /// <summary>
    /// Link to the server's web page
    /// </summary>
    public Uri? Weburl { get; set; }

    /// <summary>
    /// Server time
    /// </summary>
    public DateTime WorldTime { get; set; }

    /// <summary>
    /// Gravity. For CR-MP servers. Default value 0.008000
    /// </summary>
    public decimal Gravity { get; set; } = 0.008000M;
}