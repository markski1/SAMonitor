using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SAMonitor.Controllers;
using SAMonitor.Data;
using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

[Collection("Server startup")]
public sealed class ApiContractTests
{
    [Fact]
    public async Task CustomMasterlist_ExcludesPasswordedAndNonmatchingServers()
    {
        using var publicServer = new Server("203.0.113.1:7777") { Version = "custom 1.0" };
        using var passworded = new Server("203.0.113.2:7777") { Version = "custom 1.0", RequiresPassword = true };
        using var otherVersion = new Server("203.0.113.3:7777") { Version = "other 1.0" };
        await ServerManager.LoadServers(() => Task.FromResult(new List<Server> { publicServer, passworded, otherVersion }));
        try
        {
            Assert.Equal(publicServer.IpAddr + "\n", ServerManager.GetMasterlist("custom"));
            Assert.DoesNotContain(passworded.IpAddr, ServerManager.GetMasterlist("any"));
            Assert.Contains(otherVersion.IpAddr, ServerManager.GetMasterlist("any"));
        }
        finally
        {
            await ServerManager.LoadServers(() => Task.FromResult(new List<Server>()));
        }
    }

    [Fact]
    public async Task AddServer_PostRejectsInvalidJsonAddressAndLegacyGetKeepsText()
    {
        await using var app = await WebServer.InitializeAsync(["--urls", "http://127.0.0.1:0"]);
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(5) };
            const string path = "/api/AddServer?ip_addr=999.0.0.1";
            using var post = await client.PostAsJsonAsync("/api/AddServer", new { ipAddr = "999.0.0.1" });
            using var get = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal("application/problem+json", post.Content.Headers.ContentType?.MediaType);
            using var problem = JsonDocument.Parse(await post.Content.ReadAsStringAsync());
            Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Enter a valid IP address or hostname.", problem.RootElement.GetProperty("detail").GetString());
            Assert.Equal("Entered IP address or hostname is invalid or failing to resolve.", await get.Content.ReadAsStringAsync());

            using var missing = await client.PostAsJsonAsync("/api/AddServer", new { });
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
            using var whitespace = await client.PostAsJsonAsync("/api/AddServer", new { ipAddr = "   " });
            Assert.Equal(HttpStatusCode.BadRequest, whitespace.StatusCode);
            using var malformed = await client.PostAsync("/api/AddServer", new StringContent("{", System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
            using var wrongContentType = await client.PostAsync("/api/AddServer", new StringContent("203.0.113.1"));
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, wrongContentType.StatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task AddServer_PostReturnsConflictForMonitoredAddress()
    {
        using var server = new Server("203.0.113.1:7777");
        await ServerManager.LoadServers(() => Task.FromResult(new List<Server> { server }));
        await using var app = await WebServer.InitializeAsync(["--urls", "http://127.0.0.1:0"]);
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using var response = await client.PostAsJsonAsync("/api/AddServer", new { ipAddr = "203.0.113.1" });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Server is already monitored.", problem.RootElement.GetProperty("detail").GetString());
            Assert.Equal("Server is already monitored.", await client.GetStringAsync("/api/AddServer?ip_addr=203.0.113.1"));
        }
        finally
        {
            await app.StopAsync();
            await ServerManager.LoadServers(() => Task.FromResult(new List<Server>()));
        }
    }

    [Theory]
    [InlineData(ServerSubmissionOutcome.Blacklisted, 403)]
    [InlineData(ServerSubmissionOutcome.AlreadyMonitored, 409)]
    [InlineData(ServerSubmissionOutcome.Duplicate, 409)]
    [InlineData(ServerSubmissionOutcome.RecentlyFailed, 422)]
    [InlineData(ServerSubmissionOutcome.Unresponsive, 422)]
    [InlineData(ServerSubmissionOutcome.Unsupported, 422)]
    [InlineData(ServerSubmissionOutcome.Failed, 500)]
    public void AddServer_FailuresUseProblemDetails(ServerSubmissionOutcome outcome, int status)
    {
        var controller = new ApiController { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
        var response = Assert.IsType<ObjectResult>(controller.BuildAddServerResponse("203.0.113.1:7777", new(outcome, "Submission rejected.")));
        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(status, problem.Status);
        Assert.Equal("Submission rejected.", problem.Detail);
    }

    [Fact]
    public void AddServer_SuccessReturnsCreatedWithResourceRouteAndAddress()
    {
        var controller = new ApiController();
        var response = Assert.IsType<CreatedAtActionResult>(controller.BuildAddServerResponse("203.0.113.1:7777", new(ServerSubmissionOutcome.Added, "Server added.")));
        Assert.Equal(201, response.StatusCode);
        Assert.Equal(nameof(ApiController.GetServerByIp), response.ActionName);
        Assert.Equal("203.0.113.1:7777", response.RouteValues?["ip_addr"]);
        Assert.Equal(new AddServerResponse("203.0.113.1:7777", "Server added."), response.Value);
    }

    [Fact]
    public async Task AddServer_PostRateLimitReturns429WithRetryAfter()
    {
        await using var app = await WebServer.InitializeAsync(["--urls", "http://127.0.0.1:0"]);
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            client.DefaultRequestHeaders.Add("Origin", "https://example.test");
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var tasks = Enumerable.Range(0, 45).Select(async _ =>
            {
                try
                {
                    return await client.PostAsJsonAsync("/api/AddServer", new { ipAddr = "999.0.0.1" }, cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            });
            var responses = await Task.WhenAll(tasks);
            try
            {
                var rejected = responses.Where(response => response?.StatusCode == HttpStatusCode.TooManyRequests).ToList();
                Assert.NotEmpty(rejected);
                foreach (var response in rejected)
                {
                    Assert.True(response!.Headers.RetryAfter?.Delta > TimeSpan.Zero);
                    Assert.Equal("https://example.test", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
                    Assert.Contains("Retry-After", string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers")));
                    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                    using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    Assert.Equal(429, problem.RootElement.GetProperty("status").GetInt32());
                }
            }
            finally
            {
                foreach (var response in responses) response?.Dispose();
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
