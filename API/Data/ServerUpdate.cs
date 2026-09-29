namespace SAMonitor.Data;

internal sealed class ServerUpdate(Server server)
{
    public int Id { get; } = server.Id;
    public string IpAddr { get; } = server.IpAddr;
    public string Name { get; } = server.Name;
    public DateTime LastUpdated { get; } = server.LastUpdated;
    public bool IsOpenMp { get; } = server.IsOpenMp;
    public bool LagComp { get; } = server.LagComp;
    public string MapName { get; } = server.MapName;
    public string GameMode { get; } = server.GameMode;
    public int PlayersOnline { get; } = server.PlayersOnline;
    public int MaxPlayers { get; } = server.MaxPlayers;
    public string Website { get; } = server.Website;
    public string Version { get; } = server.Version;
    public string Language { get; } = server.Language;
    public string SampCac { get; } = server.SampCac;
    public int Weather { get; } = server.Weather;
}
