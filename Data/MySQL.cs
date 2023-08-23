using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace SAMonitor.Data
{
    public static class MySQL
    {
        public static string? ConnectionString { get; set; }
        public static bool MySQLSetup()
        {
            MySqlConnectionStringBuilder builder = new();

            try
            {
                dynamic? data = JsonConvert.DeserializeObject(File.ReadAllText($"/home/markski/sam/mysql.txt"));

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
}