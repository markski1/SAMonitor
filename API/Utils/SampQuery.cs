/*
 * THE FOLLOWING IS A SINGLE-FILE BESPOKE MODIFICATION OF SampQuery.
 *
 * This is not very good, I modified it a bunch specifically for SAMonitor.
 * !!!   And YOU should never use this for any other project, ever!
 *
 * If you wish to use SampQuery in your project, please use the actual, most excellent library by JustMavi.
 * https://github.com/justmavi/sampquery
 */

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SAMonitor.Utils;

public class SampQuery
{
    private const ushort DefaultServerPort = 7777;
    private const int ReceiveArraySize = 8192;
    private const int TimeoutMilliseconds = 5000;
    private readonly string _serverIpString;
    private readonly IPEndPoint _serverEndPoint;
    // Cache per server since destinations won't change.
    private readonly byte[] _packetPrefix;
    private DateTime _transmitMs;

    private SampQuery(string host, ushort port)
    {
        IPAddress serverIp1;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // if the given 'host' cannot be parsed as an IP Address, it might be a domain/hostname.
        if (!IPAddress.TryParse(host, out var getAddr))
        {
            serverIp1 = Dns.GetHostEntry(host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
        }
        else
        {
            serverIp1 = getAddr;
        }

        _serverEndPoint = new IPEndPoint(serverIp1, port);

        _serverIpString = serverIp1.ToString();

        // Build the per-destination packet header: "SAMP" + 4 IPv4 octets + 2-byte port.
        _packetPrefix = new byte[10];
        "SAMP"u8.CopyTo(_packetPrefix);

        var octets = _serverIpString.Split('.');
        for (int i = 0; i < 4; i++)
        {
            _packetPrefix[4 + i] = byte.Parse(octets[i], CultureInfo.InvariantCulture);
        }

        // SA-MP uses little-endian for the port field. Wtf
        _packetPrefix[8] = (byte)(port & 0xFF);
        _packetPrefix[9] = (byte)((port >> 8) & 0xFF);
    }

    public SampQuery(string ip) : this(ip.Split(':')[0], GetPortFromStringOrDefault(ip)) { }

    private static ushort GetPortFromStringOrDefault(string ip)
    {
        var parts = ip.Split(':');
        return parts.Length > 1 ? string.IsNullOrWhiteSpace(parts[1]) ? DefaultServerPort : ushort.Parse(parts[1]) : DefaultServerPort;
    }

    private async Task<byte[]> SendSocketToServerAsync(char packetType)
    {
        // Local socket so multiple async invocations on the same SampQuery instance
        // (e.g. concurrent 'i' and 'r' queries) do not race on the underlying socket.
        using var socket = new Socket(_serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        // Build the per-query packet: cached prefix + opcode byte. Avoids MemoryStream / BinaryWriter / per-call string split.
        var packet = new byte[_packetPrefix.Length + 1];
        Buffer.BlockCopy(_packetPrefix, 0, packet, 0, _packetPrefix.Length);
        packet[_packetPrefix.Length] = (byte)packetType;

        _transmitMs = DateTime.Now;

        await socket.SendToAsync(packet, SocketFlags.None, _serverEndPoint);
        EndPoint rawPoint = _serverEndPoint;
        var data = new byte[ReceiveArraySize];

        var task = socket.ReceiveFromAsync(data, SocketFlags.None, rawPoint);

        if (await Task.WhenAny(task, Task.Delay(TimeoutMilliseconds)) != task)
        {
            throw new SocketException(10060); // Operation timed out
        }

        var result = await task;
        if (result.ReceivedBytes == 0)
        {
            throw new SocketException(10060); // Empty response treated as timeout
        }

        var actualData = new byte[result.ReceivedBytes];
        Buffer.BlockCopy(data, 0, actualData, 0, result.ReceivedBytes);
        return actualData;
    }

    /// <summary>
    /// Get server players. Attempts either player list or client list.
    /// </summary>
    /// <returns>An asynchronous task that completes with the collection of ServerPlayer instances</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<List<ServerPlayer>> GetServerPlayersAsync(bool isOpenMp = false)
    {
        byte[] data;
        // open.mp does not support the 'd' (detailed player list) opcode.
        // https://github.com/openmultiplayer/open.mp/blob/master/Server/Components/LegacyNetwork/Query/query.cpp
        if (!isOpenMp)
        {
            try
            {
                data = await SendSocketToServerAsync('d');
                return CollectServerPlayersInfoFromByteArray(data, 'd');
            }
            catch
            {
                // fallback to c
            }
        }
        data = await SendSocketToServerAsync('c');
        return CollectServerPlayersInfoFromByteArray(data, 'c');
    }

    /// <summary>
    /// Get information about server
    /// </summary>
    /// <returns>An asynchronous task that completes with an instance of ServerPlayer</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<ServerInfo> GetServerInfoAsync()
    {
        byte[] data = await SendSocketToServerAsync('i');
        return CollectServerInfoFromByteArray(data);
    }

    /// <summary>
    /// Get server rules
    /// </summary>
    /// <returns>An asynchronous task that completes with an instance of ServerRules</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<ServerRules> GetServerRulesAsync()
    {
        byte[] data = await SendSocketToServerAsync('r');
        return CollectServerRulesFromByteArray(data);
    }

    private static List<ServerPlayer> CollectServerPlayersInfoFromByteArray(byte[] data, char packetType)
    {
        List<ServerPlayer> returnData = [];

        using MemoryStream stream = new(data);
        using BinaryReader read = new(stream, Encoding.GetEncoding(1251));
        read.ReadBytes(10);
        read.ReadChar();

        for (int i = 0, iTotalPlayers = read.ReadInt16(); i < iTotalPlayers; i++)
        {
            if (packetType == 'd') // if the packet type is 'd', we got a full player list.
            {
                var playerId = Convert.ToByte(read.ReadByte());
                var nameLen = read.ReadByte();
                var playerName = Encoding.GetEncoding(1251).GetString(read.ReadBytes(nameLen));
                returnData.Add(new ServerPlayer
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    PlayerScore = read.ReadInt32(),
                    PlayerPing = read.ReadInt32()
                });
            }
            else // Otherwise we got a 'client' list, which might be incomplete, as per https://open.mp/docs/tutorials/QueryMechanism
            {
                var nameLen = read.ReadByte();
                var playerName = Encoding.GetEncoding(1251).GetString(read.ReadBytes(nameLen));
                returnData.Add(new ServerPlayer
                {
                    PlayerId = 0,
                    PlayerName = playerName,
                    PlayerScore = read.ReadInt32(),
                    PlayerPing = 0
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

            ServerPing = DateTime.Now.Subtract(_transmitMs).Milliseconds
        };
    }

    private static ServerRules CollectServerRulesFromByteArray(byte[] data)
    {
        var sampServerRulesData = new ServerRules();

        using MemoryStream stream = new(data);
        using BinaryReader read = new(stream, Encoding.GetEncoding(1251));
        read.ReadBytes(10);
        read.ReadChar();

        for (int i = 0, iRules = read.ReadInt16(); i < iRules; i++)
        {
            PropertyInfo? property = sampServerRulesData.GetType().GetProperty(new string(read.ReadChars(read.ReadByte())).Replace(' ', '_'), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var value = new string(read.ReadChars(read.ReadByte()));

            if (property == null) continue;

            object val;
            if (property.PropertyType == typeof(bool)) val = value == "On";
            else if (property.PropertyType == typeof(Uri)) val = SqHelpers.ParseWebUrl(value);
            else if (property.PropertyType == typeof(DateTime)) val = SqHelpers.ParseTime(value);
            else val = SqHelpers.TryParseByte(value, property);

            property.SetValue(sampServerRulesData, val);
        }
        return sampServerRulesData;
    }
}


public static class SqHelpers
{
    public static Uri ParseWebUrl(string value)
    {
        if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var parsedUri)) return parsedUri;
        return Uri.TryCreate(value, UriKind.Absolute, out parsedUri) ? parsedUri : new Uri("https://sa-mp.mp/", UriKind.Absolute);
    }

    public static DateTime ParseTime(string value)
    {
        if (!TimeSpan.TryParse(value, out var parsedTime)) parsedTime = TimeSpan.FromHours(0);
        return DateTime.Today.Add(parsedTime);
    }

    public static object TryParseByte(string value, PropertyInfo property)
    {
        try
        {
            return Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
        }
        catch
        {
            // the value could not be parsed, try to return anything at all instead of crashing.
            return Convert.ChangeType("0", property.PropertyType, CultureInfo.InvariantCulture);
        }
    }
}


public class ServerInfo
{
    public string HostName { get; init; } = "UNKNOWN";
    public string GameMode { get; init; } = "UNKNOWN";
    public string Language { get; init; } = "UNKNOWN";
    public ushort Players { get; init; }
    public ushort MaxPlayers { get; init; }
    public bool Password { get; init; }
    public int ServerPing { get; set; } = -1;
}

public class ServerPlayer
{
    public byte PlayerId { get; init; }
    public string PlayerName { get; init; } = "UNKNOWN";
    public int PlayerScore { get; init; }
    public int PlayerPing { get; init; }
}

public class ServerRules
{
    public bool LagComp { get; set; }
    public string? MapName { get; set; }
    public string? Version { get; set; }
    public string? SampcacVersion { get; set; }
    public int Weather { get; set; }
    public Uri? WebUrl { get; set; }
    public DateTime WorldTime { get; set; }
    public decimal Gravity { get; set; } = 0.008000M;
}
