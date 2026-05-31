using MySqlConnector;

namespace SAMonitor.Utils;

public static class MySql
{
    public static string? ConnectionString { get; private set; }
    public static bool MySqlSetup()
    {
        MySqlConnectionStringBuilder builder = [];

        try
        {
            var host = Environment.GetEnvironmentVariable("MYSQL_HOST");
            var user = Environment.GetEnvironmentVariable("MYSQL_USER");
            var pass = Environment.GetEnvironmentVariable("MYSQL_PASS");
            var db   = Environment.GetEnvironmentVariable("MYSQL_DB");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) ||
                string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(db))
            {
                Console.WriteLine("Missing required MYSQL_* environment variables.");
                return false;
            }

            builder.Server = host;
            builder.UserID = user;
            builder.Password = pass;
            builder.Database = db;

            ConnectionString = builder.ToString();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to build the ConnectionString: {ex.Message}");
            return false;
        }
    }
}
