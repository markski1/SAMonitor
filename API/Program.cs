using SAMonitor.Data;
using SAMonitor.Utils;

DotEnv.Load();

if (!MySql.MySqlSetup())
{
    Console.WriteLine("Could not generate ConnectionString for MySQL.\nExiting.");
    return 1;
}

Helpers.LoadWebhookUrl();
if (!await QueryManagerProxy.SetupAsync())
{
    Console.WriteLine("Query Proxy Service is unreachable. Continuing with cached data and direct queries.");
}

Console.WriteLine("Loading servers.");
await ServerManager.LoadServers();

Console.WriteLine("Loading statistics.");
StatsManager.LoadStats();

Console.WriteLine("Initializing server updater.");
ServerUpdater.Initialize();

ThreadPool.SetMinThreads(64, 32);

await using var app = await WebServer.InitializeAsync(args);
var refresh = ServerManager.RefreshServersAsync(app.Lifetime.ApplicationStopping);
await app.WaitForShutdownAsync();
await refresh;

return 0;
