namespace SAMPQuery 
{
    /// <summary>
    /// Server Packet Types. See https://sampwiki.blast.hk/wiki/Query_Mechanism#Recieving_the_packets
    /// </summary>
    public static class ServerPacketTypes 
    {
        /// <summary>
        /// Information Packet
        /// </summary>
        /// <value>i</value>
        public static char Info => 'i';
        /// <summary>
        /// Rule packet
        /// </summary>
        /// <value>r</value>
        public static char Rules => 'r';
        /// <summary>
        /// Players packet
        /// </summary>
        /// <value>d</value>    
        public static char Players => 'd';
        /// <summary>
        /// RCON packet
        /// </summary>
        /// <value>x</value>
        public static char Rcon => 'x';
        /// <summary>
        /// OMP Packet
        /// </summary>
        /// <value>o</value>
        public static char OMP => 'o';
    }
}
