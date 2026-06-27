using SAMonitor.Data;
using SAMonitor.Database;

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

            // Dedupe by IP addr
            var deduped = new List<Server>(currentQueue.Count);
            var seen = new HashSet<string>(currentQueue.Count, StringComparer.Ordinal);
            foreach (var server in currentQueue)
            {
                if (seen.Add(server.IpAddr))
                {
                    deduped.Add(server);
                }
            }

            await ServerRepository.UpdateServersBatch(deduped);
        }
        catch (Exception ex)
        {
            await Helpers.LogError("ProcessQueue", ex);
        }
    }
}
