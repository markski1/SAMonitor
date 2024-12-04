/*
 * THIS IS A SLIGHTLY MODIFIED VERSION OF SAMPQUERY BY JUSTMAVI
 * FOR THE SAKE OF NOT CONTAMINATING THAT REPOSITORY WITH SAMONITOR-SPECIFIC CHANGES.
 * 
 * ALL CREDIT TO JUSTMAVI
 * 
 * ORIGINAL: https://github.com/justmavi/sampquery
 */

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using SAMPQuery.Utils;
using SAMPQuery;
using SAMonitor.SampQuery.Types;

namespace SAMonitor.SampQuery;

/// <summary>
/// Implements the SAMPQuery interface.
/// </summary>
public class SampQuery
{
    /// <summary>
    /// Default SAMP server port (always 7777)
    /// </summary>
    public static readonly ushort DefaultServerPort = 7777;

    private readonly int receiveArraySize = 2048;
    private readonly int timeoutMilliseconds = 5000;
    private readonly IPAddress serverIp;
    private readonly ushort serverPort;
    private readonly string serverIpString;
    private readonly IPEndPoint serverEndPoint;
    private readonly char[] socketHeader;
    private Socket? serverSocket = null;
    private DateTime transmitMS;

    /// <summary>
    /// Initialize SAMPQuery
    /// </summary>
    /// <param name="host">Server hostname or IP address</param>
    /// <param name="port">Server port</param>
    public SampQuery(string host, ushort port)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        IPAddress? getAddr = null;

        // if the given 'host' cannot be parsed as an IP Address, it might be a domain/hostname.
        if (!IPAddress.TryParse(host, out getAddr))
        {
            serverIp = Dns.GetHostEntry(host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
        }
        else
        {
            serverIp = getAddr;
        }

        serverEndPoint = new IPEndPoint(serverIp, port);

        serverIpString = serverIp.ToString();
        serverPort = port;

        socketHeader = "SAMP".ToCharArray();
    }

    /// <summary>
    /// Initialize SAMPQuery with default 7777 port or with port from given string (ip:port)
    /// </summary>
    /// <param name="ip">Server IP address</param>
    /// <returns>SampQuery instance</returns>
    public SampQuery(string ip) : this(ip.Split(':')[0], GetPortFromStringOrDefault(ip)) { }

    private static ushort GetPortFromStringOrDefault(string ip)
    {
        var parts = ip.Split(':');
        return parts.Length > 1 ? string.IsNullOrWhiteSpace(parts[1]) ? DefaultServerPort : ushort.Parse(parts[1]) : DefaultServerPort;
    }

    private async Task<byte[]> SendSocketToServerAsync(char packetType, string cmd = "")
    {
        serverSocket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        string[] splitIp = serverIpString.Split('.');

        writer.Write(socketHeader);

        for (sbyte i = 0; i < splitIp.Length; i++)
        {
            writer.Write(Convert.ToByte(Convert.ToInt16(splitIp[i])));
        }

        writer.Write(serverPort);
        writer.Write(packetType);

        transmitMS = DateTime.Now;

        await serverSocket.SendToAsync(stream.ToArray(), SocketFlags.None, serverEndPoint);
        EndPoint rawPoint = serverEndPoint;
        var data = new byte[receiveArraySize];

        var task = serverSocket.ReceiveFromAsync(data, SocketFlags.None, rawPoint);

        if (await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)) != task)
        {
            serverSocket.Close();
            throw new SocketException(10060); // Operation timed out
        }

        serverSocket.Close();
        return data;

    }
    private byte[] SendSocketToServer(char packetType)
    {
        serverSocket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            SendTimeout = timeoutMilliseconds,
            ReceiveTimeout = timeoutMilliseconds
        };

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        string[] splitIp = serverIpString.Split('.');

        writer.Write(socketHeader);

        for (sbyte i = 0; i < splitIp.Length; i++)
        {
            writer.Write(Convert.ToByte(Convert.ToInt16(splitIp[i])));
        }

        writer.Write(serverPort);
        writer.Write(packetType);

        transmitMS = DateTime.Now;

        serverSocket.SendTo(stream.ToArray(), SocketFlags.None, serverEndPoint);

        EndPoint rawPoint = serverEndPoint;
        var szReceive = new byte[receiveArraySize];

        serverSocket.ReceiveFrom(szReceive, SocketFlags.None, ref rawPoint);

        serverSocket.Close();
        return szReceive;

    }
    /// <summary>
    /// Get server players
    /// </summary>
    /// <returns>An asynchronous task that completes with the collection of ServerPlayer instances</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<IEnumerable<ServerPlayer>> GetServerPlayersAsync()
    {
        byte[] data = await SendSocketToServerAsync(ServerPacketTypes.Players);
        return CollectServerPlayersInfoFromByteArray(data);
    }
    /// <summary>
    /// Get server players
    /// </summary>
    /// <returns>Collection of ServerPlayer instances</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public IEnumerable<ServerPlayer> GetServerPlayers()
    {
        byte[] data = SendSocketToServer(ServerPacketTypes.Players);
        return CollectServerPlayersInfoFromByteArray(data);
    }
    /// <summary>
    /// Get information about server
    /// </summary>
    /// <returns>An asynchronous task that completes with an instance of ServerPlayer</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<ServerInfo> GetServerInfoAsync()
    {
        byte[] data = await SendSocketToServerAsync(ServerPacketTypes.Info);
        return CollectServerInfoFromByteArray(data);
    }
    /// <summary>
    /// Get wether the server software is open.mp or not
    /// </summary>
    /// <returns>An asynchronous task that completes with an instance of Bool</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public bool GetServerIsOMP()
    {
        try
        {
            SendSocketToServer('o');
            return true;
        }
        catch
        {
            // a timeout means the server is not open.mp
            return false;
        }
    }
    /// <summary>
    /// Get information about server
    /// </summary>
    /// <returns>An instance of ServerPlayer</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public ServerInfo GetServerInfo()
    {
        byte[] data = SendSocketToServer(ServerPacketTypes.Info);
        return CollectServerInfoFromByteArray(data);
    }
    /// <summary>
    /// Get server rules
    /// </summary>
    /// <returns>An asynchronous task that completes with an instance of ServerRules</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<ServerRules> GetServerRulesAsync()
    {
        byte[] data = await SendSocketToServerAsync(ServerPacketTypes.Rules);
        return CollectServerRulesFromByteArray(data);
    }
    /// <summary>
    /// Get server rules
    /// </summary>
    /// <returns>An instance of ServerRules</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public ServerRules GetServerRules()
    {
        byte[] data = SendSocketToServer(ServerPacketTypes.Rules);
        return CollectServerRulesFromByteArray(data);
    }

    private static IEnumerable<ServerPlayer> CollectServerPlayersInfoFromByteArray(byte[] data)
    {
        List<ServerPlayer> returnData = [];

        using (MemoryStream stream = new(data))
        {
            using BinaryReader read = new(stream);
            read.ReadBytes(10);
            read.ReadChar();

            for (int i = 0, iTotalPlayers = read.ReadInt16(); i < iTotalPlayers; i++)
            {
                returnData.Add(new ServerPlayer
                {
                    PlayerId = Convert.ToByte(read.ReadByte()),
                    PlayerName = new string(read.ReadChars(read.ReadByte())),
                    PlayerScore = read.ReadInt32(),
                    PlayerPing = read.ReadInt32()
                });
            }
        }

        return returnData;
    }

    private ServerInfo CollectServerInfoFromByteArray(byte[] data)
    {
        using MemoryStream stream = new(data);
        using BinaryReader read = new(stream, Encoding.GetEncoding(1251));
        read.ReadBytes(10);
        read.ReadChar();

        return new ServerInfo
        {
            Password = Convert.ToBoolean(read.ReadByte()),
            Players = read.ReadUInt16(),
            MaxPlayers = read.ReadUInt16(),

            HostName = new string(read.ReadChars(read.ReadInt32())),
            GameMode = new string(read.ReadChars(read.ReadInt32())),
            Language = new string(read.ReadChars(read.ReadInt32())),

            ServerPing = DateTime.Now.Subtract(transmitMS).Milliseconds,
        };
    }

    private static ServerRules CollectServerRulesFromByteArray(byte[] data)
    {
        var sampServerRulesData = new ServerRules();

        using MemoryStream stream = new(data);
        using BinaryReader read = new(stream, Encoding.GetEncoding(1251));
        read.ReadBytes(10);
        read.ReadChar();

        string value;
        object val;

        for (int i = 0, iRules = read.ReadInt16(); i < iRules; i++)
        {
            PropertyInfo? property = sampServerRulesData.GetType().GetProperty(new string(read.ReadChars(read.ReadByte())).Replace(' ', '_'), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            value = new string(read.ReadChars(read.ReadByte()));

            if (property != null)
            {
                if (property.PropertyType == typeof(bool)) val = value == "On";
                else if (property.PropertyType == typeof(Uri)) val = Helpers.ParseWebUrl(value);
                else if (property.PropertyType == typeof(DateTime)) val = Helpers.ParseTime(value);
                else val = Helpers.TryParseByte(value, property);

                property.SetValue(sampServerRulesData, val);
            }
        }
        return sampServerRulesData;
    }
}
