using MySqlConnector;
using SAMonitor.Data;
using SAMonitor.Utils;
using static Dapper.SqlMapper;

namespace SAMonitor.Database
{
    public interface IServerRepository
    {
        Task<List<Server>> GetAllServersAsync();
        Task<int> GetServerId(string ipAddr);
        Task<bool> InsertServer(Server server);
        Task<bool> UpdateServer(Server server);
    }
    public class ServerRepository
    {
        private static readonly List<MySqlConnection> _availableConnections = [];

        private static readonly object _lock = new object();

        private static MySqlConnection GetConnection()
        {
            lock (_lock)
            {
                List<MySqlConnection> removal = [];

                MySqlConnection? connection = null;

                // Get an open connection from the available connection pool.
                foreach (MySqlConnection conn in _availableConnections)
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        connection = conn;
                        break;
                    }

                    // Broken or closed connections are added to a removal list.
                    // Because _availableConnections cannot be mutated while being iterated.
                    if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Broken)
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
                    return connection;
                }

                // Otherwise, create a new connection.
                var newConn = new MySqlConnection(MySql.ConnectionString);

                return newConn;
            }
        }

        private static void ReturnConnection(MySqlConnection connection)
        {
            lock (_lock)
            {
                _availableConnections.Add(connection);
            }
        }

        public async Task<List<Server>> GetAllServersAsync()
        {
            var db = GetConnection();

            const string sql = @"SELECT id, ip_addr, name, last_updated, is_open_mp, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac, sponsor_until FROM servers";

            try
            {
                var result = (await db.QueryAsync<Server>(sql)).ToList();
                
                ReturnConnection(db);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[db_err] Could not get servers from database \n {ex}");
                return new List<Server>();
            }
        }

        public async Task<int> GetServerId(string ipAddr)
        {
            var db = GetConnection();

            var sql = @"SELECT id FROM servers WHERE ip_addr=@IpAddr";

            try
            {
                var result = (await db.QueryAsync<int>(sql, new { IpAddr = ipAddr })).Single();
                
                ReturnConnection(db);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[db_err] Failed to get ID for {ipAddr} !! \n {ex}");
                return 0;
            }
        }

        public async Task<bool> InsertServer(Server server)
        {
            var db = GetConnection();

            string sql = @"INSERT INTO servers (ip_addr, name, last_updated, is_open_mp, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac)
                        VALUES(@IpAddr, @Name, @LastUpdated, @IsOpenMp, @LagComp, @MapName, @GameMode, @PlayersOnline, @MaxPlayers, @Website, @Version, @Language, @SampCac)";

            try
            {
                var result = (await db.ExecuteAsync(sql, new
                {
                    server.IpAddr,
                    server.Name,
                    server.LastUpdated,
                    server.IsOpenMp,
                    server.LagComp,
                    server.MapName,
                    server.GameMode,
                    server.PlayersOnline,
                    server.MaxPlayers,
                    server.Website,
                    server.Version,
                    server.Language,
                    server.SampCac
                })) > 0;

                ReturnConnection(db);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[db_err] Failed to add {server.IpAddr} to the database \n {ex}");
                return false;
            }
        }

        public async Task<int> InsertServerMetrics(int server_id, int player_amount)
        {
            var db = GetConnection();

            string sql = @"INSERT INTO metrics_server (server_id, players) VALUES (@server_id, @player_amount)";

            var result = await db.ExecuteAsync(sql, new { server_id, player_amount });
            
            ReturnConnection(db);
            return result;
        }

        public async Task<bool> UpdateServer(Server server)
        {
            var db = GetConnection();

            var sql = @"UPDATE servers
                        SET name=@Name, last_updated=@LastUpdated, is_open_mp=@IsOpenMp, lag_comp=@LagComp, map_name=@MapName, gamemode=@GameMode, players_online=@PlayersOnline, max_players=@MaxPlayers, website=@Website, version=@Version, language=@Language, sampcac=@SampCac
                        WHERE ip_addr = @IpAddr";

            bool success;

            try
            {
                success = (await db.ExecuteAsync(sql, new
                {
                    server.IpAddr,
                    server.Name,
                    server.LastUpdated,
                    server.IsOpenMp,
                    server.LagComp,
                    server.MapName,
                    server.GameMode,
                    server.PlayersOnline,
                    server.MaxPlayers,
                    server.Website,
                    server.Version,
                    server.Language,
                    server.SampCac
                })) > 0;
            }
            catch
            {
                success = false;
            }

            // then add a metric entry. ONLY IF IN PRODUCTION.

            if (Global.IsDevelopment)
            {
                ReturnConnection(db);
                return success;
            }
            
            sql = @"INSERT INTO metrics_server (server_id, players) VALUES (@Id, @PlayersOnline)";

            await db.ExecuteAsync(sql, new { server.Id, server.PlayersOnline });

            ReturnConnection(db);
            return success;
        }

        public async Task<List<ServerMetrics>> GetServerMetrics(int id, DateTime requestTime, int include_misses = 0)
        {
            var db = GetConnection();

            string sql;

            if (include_misses > 0)
            {
                sql = @"SELECT players, time FROM metrics_server WHERE time > @requestTime AND server_id = @id ORDER BY time DESC";
            }
            else
            {
                sql = @"SELECT players, time FROM metrics_server WHERE time > @requestTime AND server_id = @id AND players >= 0 ORDER BY time DESC";
            }

            var result = (await db.QueryAsync<ServerMetrics>(sql, new { requestTime, id })).ToList();

            ReturnConnection(db);
            return result;
        }
    }
}
