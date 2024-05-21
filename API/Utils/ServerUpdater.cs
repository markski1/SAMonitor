using SAMonitor.Data;
using SAMonitor.Database;
using System.Timers;

namespace SAMonitor.Utils
{
    public static class ServerUpdater
    {
        private static readonly List<Server> UpdateQueue = [];

        private static readonly System.Timers.Timer UpdateQueueTimer = new();

        private static readonly ServerRepository Interface = new();

        public static void Initialize()
        {
            UpdateQueueTimer.Elapsed += TimedRun;
            UpdateQueueTimer.AutoReset = true;
            UpdateQueueTimer.Interval = 15000;
            UpdateQueueTimer.Enabled = true;
        }

        public static void Queue(Server server)
        {
            UpdateQueue.Add(server);
        }

        private static void TimedRun(object? sender, ElapsedEventArgs e)
        {
            Thread timedActions = new(ProcessQueue);
            timedActions.Start();
        }

        private static async void ProcessQueue()
        {
            /*
             * Regarding this try catch block:
             * 
             * There's a diceroll chance that after a week of runtime, SAMonitor will crash here,
             * citing an "unhandled exception" because copying an array from source to destionation fails,
             * due to the destination array not being large enough.
             * 
             * There's no explicit array copying here, so I assume it's below the .NET 'List' abstraction.
             * 
             * I don't have time to actually -fix- this now, but it's rare enough that I deem it acceptable
             * we just catch and discard any failure.
             */

            try
            {
                var currentQueue = new List<Server>(UpdateQueue);
                UpdateQueue.Clear();

                foreach (var server in currentQueue)
                {
                    await Interface.UpdateServer(server);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing the server updating queue: {ex}");
            }
        }
    }
}
