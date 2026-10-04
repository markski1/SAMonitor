using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using SAMonitor.Data;
using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

[CollectionDefinition("Server startup", DisableParallelization = true)]
public sealed class ServerStartupCollection;

[Collection("Server startup")]
public sealed class ServerStartupTests
{
    [Fact]
    public async Task Api_ServesCachedDataWhileInitialQueriesArePending()
    {
        using var endpoint = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)endpoint.Client.LocalEndPoint!).Port;
        using var recent = new Server($"127.0.0.1:{port}")
        {
            Id = 1, Name = "Cached server", LastUpdated = DateTime.UtcNow.AddHours(-1), PlayersOnline = 10
        };
        using var expiredEndpoint = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int expiredPort = ((IPEndPoint)expiredEndpoint.Client.LocalEndPoint!).Port;
        using var expired = new Server($"127.0.0.1:{expiredPort}")
        {
            Id = 2, LastUpdated = DateTime.UtcNow.AddHours(-7)
        };
        using var unnamedEndpoint = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int unnamedPort = ((IPEndPoint)unnamedEndpoint.Client.LocalEndPoint!).Port;
        using var unnamed = new Server($"127.0.0.1:{unnamedPort}") { Id = 3, Name = "" };
        await ServerManager.LoadServers(() => Task.FromResult(new List<Server> { recent, expired, unnamed }));

        Assert.Same(recent, Assert.Single(ServerManager.GetServers()));
        Assert.Equal(3, ServerManager.GetAllServers().Count);
        Assert.Same(recent, ServerManager.ServerByIp(recent.IpAddr));
        Assert.Equal(recent.IpAddr + Environment.NewLine, ServerManager.GetMasterlist("any"));

        await using var app = await WebServer.InitializeAsync(["--urls", "http://127.0.0.1:0"]);
        using var cancellation = new CancellationTokenSource();
        var refresh = ServerManager.RefreshServersAsync(cancellation.Token);
        try
        {
            using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await endpoint.ReceiveAsync(receiveTimeout.Token);
            Assert.False(refresh.IsCompleted);

            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(5) };
            Assert.Equal("SAMonitor lives!", await client.GetStringAsync("/api/CheckAlive"));
            using var servers = JsonDocument.Parse(await client.GetStringAsync("/api/GetAllServers"));
            var cached = Assert.Single(servers.RootElement.EnumerateArray());
            Assert.Equal("Cached server", cached.GetProperty("name").GetString());
            Assert.Equal(10, cached.GetProperty("playersOnline").GetInt32());
            Assert.False(refresh.IsCompleted);
        }
        finally
        {
            cancellation.Cancel();
            await refresh.WaitAsync(TimeSpan.FromSeconds(5));
            await app.StopAsync();
            await ServerManager.LoadServers(() => Task.FromResult(new List<Server>()));
        }
    }
}
