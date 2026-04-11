using SAMonitor.Data;
using SAMonitor.Utils;

if (!MySql.MySqlSetup())
{
    Console.WriteLine("Could not generate ConnectionString for MySQL.\nExiting.");
    return 1;
}

Helpers.LoadWebhookUrl();
if (!await QueryManagerProxy.SetupAsync())
{
    Console.WriteLine("Query Proxy Service is configured but unreachable.\nExiting.");
    return 1;
}

Console.WriteLine("Loading servers.");
await ServerManager.LoadServers();

Console.WriteLine("Loading statistics.");
StatsManager.LoadStats();

Console.WriteLine("Initializing server updater.");
ServerUpdater.Initialize();

WebServer.Initialize(args);

ThreadPool.SetMinThreads(64, 32);
ThreadPool.SetMaxThreads(256, 128);

await Task.Delay(-1);

return 0;