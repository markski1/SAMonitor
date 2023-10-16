namespace SAMPQuery
{
    /// <summary>
    /// Server Information
    /// </summary>
    public class ServerInfo
    {
        /// <summary>
        /// Hostname
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gamemode text
        /// </summary>
        public string? GameMode { get; set; }

        /// <summary>
        /// Server language 
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Number of players online
        /// </summary>
        public ushort Players { get; set; }

        /// <summary>
        /// Maximum number of players 
        /// </summary>
        public ushort MaxPlayers { get; set; }

        /// <summary>
        /// Password availability
        /// </summary>
        public bool Password { get; set; }

        /// <summary>
        /// Ping of server
        /// </summary>
        public int ServerPing { get; set; }
    }
}