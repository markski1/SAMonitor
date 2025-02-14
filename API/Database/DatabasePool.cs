using MySqlConnector;
using SAMonitor.Utils;

namespace SAMonitor.Database;

public static class DatabasePool
{
    private static readonly List<MySqlConnection> AvailableConnections = [];

    private static readonly Lock Lock = new();

    public static DbConnectionWrapper GetConnection()
    {
        lock (Lock)
        {
            List<MySqlConnection> removal = [];

            MySqlConnection? connection = null;

            // Get an open connection from the available connection pool.
            foreach (MySqlConnection conn in AvailableConnections)
            {
                // If a connection is open, or has been closed normally, it can be used.
                if (conn.State is System.Data.ConnectionState.Open or System.Data.ConnectionState.Closed)
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
                AvailableConnections.Remove(conn);
                conn.Dispose();
            }

            // If a connection was found, remove it from the available list and return it.
            if (connection != null)
            {
                AvailableConnections.Remove(connection);
                return new DbConnectionWrapper(connection);
            }

            // Otherwise, create a new connection.
            var newConn = new MySqlConnection(MySql.ConnectionString);

            return new DbConnectionWrapper(newConn);
        }
    }

    public static void ReturnConnection(MySqlConnection connection)
    {
        lock (Lock)
        {
            AvailableConnections.Add(connection);
        }
    }
}

public class DbConnectionWrapper(MySqlConnection connection) : IDisposable
{
    public readonly MySqlConnection Db = connection;

    public void Dispose()
    {
        DatabasePool.ReturnConnection(Db);
        GC.SuppressFinalize(this);
    }
}