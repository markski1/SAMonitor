using SAMonitor.Data;
using SAMonitor.Utils;
using System.Diagnostics;

if (MySql.MySqlSetup())
{
    Console.WriteLine("Loading servers.");
    await ServerManager.LoadServers();

    Console.WriteLine("Loading statistics.");
    StatsManager.LoadStats();

    Console.WriteLine("Initializing server updater.");
    ServerUpdater.Initialize();

    WebServer.Initialize(args);

    await Task.Delay(-1);
}
else
{
    Console.WriteLine("Could not generate ConnectionString for MySQL.\nExiting.");
}

