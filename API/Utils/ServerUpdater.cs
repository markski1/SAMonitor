using MySqlConnector;
using SAMonitor.Data;
using SAMonitor.Database;
using System.Timers;

namespace SAMonitor.Utils
{
    public static class ServerUpdater
    {
        private static readonly List<Server> UpdateQueue = new();

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
            var currentQueue = new List<Server>(UpdateQueue);
            UpdateQueue.Clear();

            MySqlConnection db = new(MySql.ConnectionString);

            foreach (var server in currentQueue)
            {
                await Interface.UpdateServer(server, db);
            }
        }
    }
}
