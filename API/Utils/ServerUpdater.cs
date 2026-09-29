using SAMonitor.Data;
using SAMonitor.Database;

namespace SAMonitor.Utils;

public static class ServerUpdater
{
    private static readonly ServerUpdateQueue PendingUpdates = new(ServerRepository.UpdateServersBatch);

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
                await PendingUpdates.ProcessAsync();
            }
            catch (Exception ex)
            {
                await Helpers.LogError("UpdateQueueLoop", ex);
            }
        }
    }

    public static void Queue(Server server)
    {
        PendingUpdates.Enqueue(server);
    }
}
