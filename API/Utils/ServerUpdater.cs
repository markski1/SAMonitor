using SAMonitor.Data;
using SAMonitor.Database;
using System.Timers;

namespace SAMonitor.Utils;

public static class ServerUpdater
{
    private static readonly List<Server> UpdateQueue = [];
    private static readonly Lock Lock = new();

    public static void Initialize()
    {
        Task.Run(UpdateQueueLoop);
    }

    private static async Task UpdateQueueLoop()
    {
        while (true)
        {
            await Task.Delay(15000);
            try
            {
                await ProcessQueue();
            }
            catch (Exception ex)
            {
                await Helpers.LogError("UpdateQueueLoop", ex);
            }
        }
    }

    public static void Queue(Server server)
    {
        lock (Lock)
        {
            UpdateQueue.Add(server);
        }
    }

    private static async Task ProcessQueue()
    {
        try
        {
            List<Server> currentQueue;
            lock (Lock)
            {
                if (UpdateQueue.Count == 0) return;
                currentQueue = new List<Server>(UpdateQueue);
                UpdateQueue.Clear();
            }
            foreach (var server in currentQueue)
            {
                if (!await ServerRepository.UpdateServer(server))
                {
                    // In the environment SAMonitor runs, messages to console are logged.
                    Console.WriteLine($"Failed to update server {server.IpAddr}");
                }
            }
        }
        catch (Exception ex)
        {
            await Helpers.LogError("ProcessQueue", ex);
        }
    }
}