using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace SAMonitor.Data
{
    public static class MySQL
    {
        public static string? ConnectionString = null;
        public static void MySQLSetup()
        {
            MySqlConnectionStringBuilder builder = new();

            dynamic? data = JsonConvert.DeserializeObject(File.ReadAllText($"/keys/mysql.txt"));

            if (data is null)
                return;

            builder.Server = data.Server;
            builder.UserID = data.UserID;
            builder.Password = data.Password;
            builder.Database = data.Database;

            ConnectionString = builder.ToString();
        }
    }
}