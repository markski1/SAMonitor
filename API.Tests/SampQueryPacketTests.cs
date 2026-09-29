using System.Net;
using System.Net.Sockets;
using System.Text;
using SAMonitor.Utils;
using SAMonitor.Data;
using Xunit;

namespace SAMonitor.Tests;

public sealed class SampQueryPacketTests
{
    private readonly SampQuery _query = new("127.0.0.1:7777");

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_000)]
    public void Info_RejectsUnboundedStringLengths(int length)
    {
        var packet = Packet('i', writer =>
        {
            WriteInfoPrefix(writer);
            writer.Write(length);
        });

        Assert.Throws<InvalidDataException>(() => _query.CollectServerInfoFromByteArray(packet));
    }

    [Fact]
    public void Info_RejectsInvalidHeaders()
    {
        var packet = InfoPacket();
        packet[0] = (byte)'X';
        Assert.Throws<InvalidDataException>(() => _query.CollectServerInfoFromByteArray(packet));
        packet[0] = (byte)'S';
        packet[10] = (byte)'r';
        Assert.Throws<InvalidDataException>(() => _query.CollectServerInfoFromByteArray(packet));
        Assert.Throws<InvalidDataException>(() => _query.CollectServerInfoFromByteArray([]));
    }

    [Theory]
    [InlineData('r')]
    [InlineData('c')]
    [InlineData('d')]
    public void Lists_RejectCountsLargerThanThePayload(char opcode)
    {
        var packet = Packet(opcode, writer => writer.Write(ushort.MaxValue));

        Assert.Throws<InvalidDataException>(() =>
        {
            if (opcode == 'r') SampQuery.CollectServerRulesFromByteArray(packet);
            else SampQuery.CollectServerPlayersInfoFromByteArray(packet, opcode);
        });
    }

    [Fact]
    public void Info_ParsesCyrillicAndEmptyStrings()
    {
        var info = _query.CollectServerInfoFromByteArray(InfoPacket());

        Assert.Equal("Русский сервер", info.HostName);
        Assert.Equal("", info.GameMode);
        Assert.Equal("Russian", info.Language);
        Assert.Equal((ushort)3, info.Players);
        Assert.Equal((ushort)10, info.MaxPlayers);
    }

    [Fact]
    public void Rules_ParseStringAndBooleanValues()
    {
        var packet = Packet('r', writer =>
        {
            writer.Write((ushort)2);
            WriteString(writer, "version", shortLength: true);
            WriteString(writer, "omp 1.0", shortLength: true);
            WriteString(writer, "lagcomp", shortLength: true);
            WriteString(writer, "On", shortLength: true);
        });
        var rules = SampQuery.CollectServerRulesFromByteArray(packet);

        Assert.Equal("omp 1.0", rules.Version);
        Assert.True(rules.LagComp);
    }

    [Theory]
    [InlineData('c')]
    [InlineData('d')]
    public void Players_ParseBothListFormats(char opcode)
    {
        var packet = Packet(opcode, writer =>
        {
            writer.Write((ushort)1);
            if (opcode == 'd') writer.Write((byte)7);
            WriteString(writer, "Alice", shortLength: true);
            writer.Write(99);
            if (opcode == 'd') writer.Write(42);
        });
        var player = Assert.Single(SampQuery.CollectServerPlayersInfoFromByteArray(packet, opcode));

        Assert.Equal("Alice", player.PlayerName);
        Assert.Equal(99, player.PlayerScore);
        Assert.Equal(opcode == 'd' ? 7 : 0, player.PlayerId);
        Assert.Equal(opcode == 'd' ? 42 : 0, player.PlayerPing);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Socket_AcceptsOnlyRepliesFromTheRequestedServer(bool wrongSender, bool wrongAddress)
    {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        using var otherSender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        var query = new SampQuery($"127.0.0.1:{port}");
        var result = query.GetServerInfoAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var request = await server.ReceiveAsync(timeout.Token);
        var response = InfoPacket();
        request.Buffer.CopyTo(response, 0);
        if (wrongAddress) response[4] ^= 1;
        await (wrongSender ? otherSender : server).SendAsync(response, request.RemoteEndPoint);

        if (wrongSender || wrongAddress)
            await Assert.ThrowsAsync<InvalidDataException>(() => result);
        else
            Assert.Equal("Русский сервер", (await result).HostName);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(30, false)]
    public async Task ServerQuery_ValidatesBeforeChangingState(byte playerCount, bool valid)
    {
        using var endpoint = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)endpoint.Client.LocalEndPoint!).Port;
        using var server = new Server($"127.0.0.1:{port}")
        {
            Id = 1, Name = "Previous name", PlayersOnline = 1, MaxPlayers = 10, IsProxyQueried = true,
            LastUpdated = DateTime.UtcNow.AddHours(-1)
        };
        var lastUpdated = server.LastUpdated;
        var query = server.Query(false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var request = await endpoint.ReceiveAsync(timeout.Token);
        var info = InfoPacket();
        request.Buffer.CopyTo(info, 0);
        info[12] = playerCount;
        await endpoint.SendAsync(info, request.RemoteEndPoint);
        if (valid)
        {
            var rulesRequest = await endpoint.ReceiveAsync(timeout.Token);
            var rules = Packet('r', writer => writer.Write((ushort)0));
            rulesRequest.Buffer.CopyTo(rules, 0);
            await endpoint.SendAsync(rules, rulesRequest.RemoteEndPoint);
        }

        Assert.Equal(valid, await query);
        Assert.Equal(valid ? "Русский сервер" : "Previous name", server.Name);
        Assert.Equal(valid ? 3 : 1, server.PlayersOnline);
        Assert.Equal(!valid, server.IsProxyQueried);
        if (valid) Assert.True(server.LastUpdated > lastUpdated);
        else Assert.Equal(lastUpdated, server.LastUpdated);
    }

    private static byte[] InfoPacket() => Packet('i', writer =>
    {
        WriteInfoPrefix(writer);
        WriteString(writer, "Русский сервер");
        WriteString(writer, "");
        WriteString(writer, "Russian");
    });

    private static byte[] Packet(char opcode, Action<BinaryWriter> writePayload)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("SAMP"u8);
        writer.Write(IPAddress.Loopback.GetAddressBytes());
        writer.Write((ushort)7777);
        writer.Write((byte)opcode);
        writePayload(writer);
        return stream.ToArray();
    }

    private static void WriteInfoPrefix(BinaryWriter writer)
    {
        writer.Write((byte)0);
        writer.Write((ushort)3);
        writer.Write((ushort)10);
    }

    private static void WriteString(BinaryWriter writer, string value, bool shortLength = false)
    {
        var bytes = Encoding.GetEncoding(1251).GetBytes(value);
        if (shortLength) writer.Write((byte)bytes.Length);
        else writer.Write(bytes.Length);
        writer.Write(bytes);
    }
}
