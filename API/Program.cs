using SAMonitor.Data;
using SAMonitor.Utils;

if (MySql.MySqlSetup())
{
    Console.WriteLine("Loading servers.");
    await ServerManager.LoadServers();

    Console.WriteLine("Loading statistics.");
    StatsManager.LoadStats();

    Console.WriteLine("Initializing server updater.");
    ServerUpdater.Initialize();

    WebServer.Initialize(args);

    // This is a maximum. Yes, it is excessive, but I'm trying to live-diagnose
    // what I suspect to be thread starvation. Cannot reproduce in development.
    ThreadPool.SetMaxThreads(5000, 1000);

    await Task.Delay(-1);
}
else
{
    Console.WriteLine("Could not generate ConnectionString for MySQL.\nExiting.");
}
