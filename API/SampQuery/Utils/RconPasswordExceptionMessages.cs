namespace SAMPQuery
{
    /// <summary>
    /// RCON error messages
    /// </summary>
    public static class RconPasswordExceptionMessages
    {
        /// <summary>
        /// Error message for RconPasswordException, when RCON password is "changeme"
        /// </summary>
        public const string CHANGEME_NOT_ALLOWED = "\"changeme\" is not allowed RCON password";
        /// <summary>
        /// Error message for RconPasswordException, when RCON password is incorrect
        /// </summary>
        public const string INVALD_RCON_PASSWORD = "Invalid RCON password";
    }
}