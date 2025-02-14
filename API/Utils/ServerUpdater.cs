using SAMonitor.Data;
using SAMonitor.Database;
using System.Timers;

namespace SAMonitor.Utils;

public static class ServerUpdater
{
    private static readonly List<Server> UpdateQueue = [];
    private static readonly Lock Lock = new();
    private static readonly System.Timers.Timer UpdateQueueTimer = new();

    public static void Initialize()
    {
        UpdateQueueTimer.Elapsed += TimedRun;
        UpdateQueueTimer.AutoReset = true;
        UpdateQueueTimer.Interval = 15000;
        UpdateQueueTimer.Enabled = true;
    }

    public static void Queue(Server server)
    {
        lock (Lock)
        {
            UpdateQueue.Add(server);
        }
    }

    private static void TimedRun(object? sender, ElapsedEventArgs e)
    {
        Thread timedActions = new(ProcessQueue);
        timedActions.Start();
    }

    private static async void ProcessQueue()
    {
        try
        {
            List<Server> currentQueue;
            lock (Lock)
            {
                currentQueue = new List<Server>(UpdateQueue);
                UpdateQueue.Clear();
            }
            foreach (var server in currentQueue)
            {
                await ServerRepository.UpdateServer(server);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error committing db changes: {ex.Message}");
        }
    }
}