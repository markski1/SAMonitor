using MySqlConnector;
using SAMonitor.Data;
using System.Timers;

namespace SAMonitor.Utils
{
    public static class ServerUpdater
    {
        private static readonly List<Server> UpdateQueue = new();

        private static readonly System.Timers.Timer UpdateQueueTimer = new();
        public static void Initialize()
        {
            UpdateQueueTimer.Elapsed += ProcessQueue;
            UpdateQueueTimer.AutoReset = true;
            UpdateQueueTimer.Interval = 10000;
            UpdateQueueTimer.Enabled = true;
        }

        public static void Queue(Server server) {
            UpdateQueue.Add(server);
        }

        private static void ProcessQueue(object? sender, ElapsedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                var CurrentQueue = new List<Server>(UpdateQueue);
                UpdateQueue.Clear();

                MySqlConnection db = new(MySQL.ConnectionString);

                foreach (var server in CurrentQueue)
                {
                    await ServerManager._interface.UpdateServer(server, db);
                }
            });
        }
    }
}
