using MySqlConnector;
using SAMonitor.Utils;

namespace SAMonitor.Database;

public static class DatabasePool
{
    public static async Task<MySqlConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(MySql.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    // Deprecated. Use GetConnectionAsync for better reliability.
    public static DbConnectionWrapper GetConnection()
    {
        var connection = new MySqlConnection(MySql.ConnectionString);
        connection.Open();
        return new DbConnectionWrapper(connection);
    }
}

public class DbConnectionWrapper(MySqlConnection connection) : IDisposable
{
    public readonly MySqlConnection Db = connection;

    public void Dispose()
    {
        Db.Dispose();
        GC.SuppressFinalize(this);
    }
}