using MySqlConnector;
using Newtonsoft.Json;

namespace SAMonitor.Utils;

public static class MySql
{
    public static string? ConnectionString { get; private set; }
    public static bool MySqlSetup()
    {
        MySqlConnectionStringBuilder builder = [];

        try
        {
            dynamic? data = JsonConvert.DeserializeObject(File.ReadAllText("/home/markski/samonitor/api/mysql.txt"));

            if (data is null)
                return false;

            builder.Server = data.Server;
            builder.UserID = data.UserID;
            builder.Password = data.Password;
            builder.Database = data.Database;

            ConnectionString = builder.ToString();

            return true;
        }
        catch
        {
            return false;
        }
    }
}