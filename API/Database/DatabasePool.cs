using MySqlConnector;
using SAMonitor.Utils;

namespace SAMonitor.Database;

public static class DatabasePool
{
    private static readonly List<MySqlConnection> _availableConnections = [];

    private static readonly object _lock = new();

    public static DbConnectionWrapper GetConnection()
    {
        lock (_lock)
        {
            List<MySqlConnection> removal = [];

            MySqlConnection? connection = null;

            // Get an open connection from the available connection pool.
            foreach (MySqlConnection conn in _availableConnections)
            {
                // If a connection is open, or has been closed normally, it can be used.
                if (conn.State == System.Data.ConnectionState.Open || conn.State == System.Data.ConnectionState.Closed)
                {
                    connection = conn;
                    break;
                }

                // Broken connections are added to a removal list.
                // Because _availableConnections cannot be mutated while being iterated.
                if (conn.State == System.Data.ConnectionState.Broken)
                {
                    removal.Add(conn);
                }
            }

            // Remove all connections in the removal list, if any.
            foreach (MySqlConnection conn in removal)
            {
                _availableConnections.Remove(conn);
                conn.Dispose();
            }

            // If a connection was found, remove it from the available list and return it.
            if (connection != null)
            {
                _availableConnections.Remove(connection);
                return new DbConnectionWrapper(connection);
            }

            // Otherwise, create a new connection.
            var newConn = new MySqlConnection(MySql.ConnectionString);

            return new DbConnectionWrapper(newConn);
        }
    }

    public static void ReturnConnection(MySqlConnection connection)
    {
        lock (_lock)
        {
            _availableConnections.Add(connection);
        }
    }
}

public class DbConnectionWrapper(MySqlConnection connection) : IDisposable
{
    public MySqlConnection db = connection;

    public void Dispose()
    {
        DatabasePool.ReturnConnection(db);
        GC.SuppressFinalize(this);
    }
}