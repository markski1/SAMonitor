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

        public static readonly ServerRepository _interface = new();

        public static void Initialize()
        {
            UpdateQueueTimer.Elapsed += TimedRun;
            UpdateQueueTimer.AutoReset = true;
            UpdateQueueTimer.Interval = 15000;
            UpdateQueueTimer.Enabled = true;
        }

        public static void Queue(Server server) {
            UpdateQueue.Add(server);
        }

        private static void TimedRun(object? sender, ElapsedEventArgs e) {
            Thread timedActions = new(ProcessQueue);
            Console.WriteLine("3");
            timedActions.Start();
            Console.WriteLine("4");
        }

        private static async void ProcessQueue()
        {
            Console.WriteLine("1");
            var CurrentQueue = new List<Server>(UpdateQueue);
            UpdateQueue.Clear();

            MySqlConnection db = new(MySQL.ConnectionString);

            foreach (var server in CurrentQueue)
            {
                await _interface.UpdateServer(server, db);
            }
            Console.WriteLine("2");
        }
    }
}
