/*
 * THE FOLLOWING IS A SINGLE-FILE BESPOKE MODIFICATION OF SampQuery.
 *
 * This is not very good, I modified it a bunch specifically for SAMonitor.
 * !!!   and YOU should never use this for any other project, ever   !!!
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
    private const int ReceiveArraySize = 2048;
    private const int TimeoutMilliseconds = 5000;
    private readonly ushort _serverPort;
    private readonly string _serverIpString;
    private readonly IPEndPoint _serverEndPoint;
    private readonly char[] _socketHeader;
    private Socket? _serverSocket;
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
        _serverPort = port;

        _socketHeader = "SAMP".ToCharArray();
    }

    public SampQuery(string ip) : this(ip.Split(':')[0], GetPortFromStringOrDefault(ip)) { }

    private static ushort GetPortFromStringOrDefault(string ip)
    {
        var parts = ip.Split(':');
        return parts.Length > 1 ? string.IsNullOrWhiteSpace(parts[1]) ? DefaultServerPort : ushort.Parse(parts[1]) : DefaultServerPort;
    }

    private async Task<byte[]> SendSocketToServerAsync(char packetType)
    {
        _serverSocket = new Socket(_serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        using var stream = new MemoryStream();
        await using var writer = new BinaryWriter(stream);
        string[] splitIp = _serverIpString.Split('.');

        writer.Write(_socketHeader);

        for (sbyte i = 0; i < splitIp.Length; i++)
        {
            writer.Write(Convert.ToByte(Convert.ToInt16(splitIp[i])));
        }

        writer.Write(_serverPort);
        writer.Write(packetType);

        _transmitMs = DateTime.Now;

        await _serverSocket.SendToAsync(stream.ToArray(), SocketFlags.None, _serverEndPoint);
        EndPoint rawPoint = _serverEndPoint;
        var data = new byte[ReceiveArraySize];

        var task = _serverSocket.ReceiveFromAsync(data, SocketFlags.None, rawPoint);

        if (await Task.WhenAny(task, Task.Delay(TimeoutMilliseconds)) != task)
        {
            _serverSocket.Close();
            throw new SocketException(10060); // Operation timed out
        }

        _serverSocket.Close();
        return data;

    }
    
    private void SendSocketToServer(char packetType)
    {
        _serverSocket = new Socket(_serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            SendTimeout = TimeoutMilliseconds,
            ReceiveTimeout = TimeoutMilliseconds
        };

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        string[] splitIp = _serverIpString.Split('.');

        writer.Write(_socketHeader);

        for (sbyte i = 0; i < splitIp.Length; i++)
        {
            writer.Write(Convert.ToByte(Convert.ToInt16(splitIp[i])));
        }

        writer.Write(_serverPort);
        writer.Write(packetType);

        _transmitMs = DateTime.Now;

        _serverSocket.SendTo(stream.ToArray(), SocketFlags.None, _serverEndPoint);

        EndPoint rawPoint = _serverEndPoint;
        var szReceive = new byte[ReceiveArraySize];
        _serverSocket.ReceiveFrom(szReceive, SocketFlags.None, ref rawPoint);
        _serverSocket.Close();
    }
    
    /// <summary>
    /// Get server players
    /// </summary>
    /// <returns>An asynchronous task that completes with the collection of ServerPlayer instances</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<IEnumerable<ServerPlayer>> GetServerPlayersAsync()
    {
        byte[] data = await SendSocketToServerAsync('d');
        return CollectServerPlayersInfoFromByteArray(data);
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
    /// Get whether the server software is open.mp or not
    /// </summary>
    /// <returns>An asynchronous task that completes with an instance of Bool</returns>
    public bool GetServerIsOmp()
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
    /// Get server rules
    /// </summary>
    /// <returns>An asynchronous task that completes with an instance of ServerRules</returns>
    /// <exception cref="SocketException">Thrown when operation timed out</exception>
    public async Task<ServerRules> GetServerRulesAsync()
    {
        byte[] data = await SendSocketToServerAsync('r');
        return CollectServerRulesFromByteArray(data);
    }

    private static IEnumerable<ServerPlayer> CollectServerPlayersInfoFromByteArray(byte[] data)
    {
        List<ServerPlayer> returnData = [];

        using MemoryStream stream = new(data);
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

            ServerPing = DateTime.Now.Subtract(_transmitMs).Milliseconds,
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


internal static class SqHelpers
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
