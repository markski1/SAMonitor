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
        private static MySqlConnection DbConnection()
        {
            return new MySqlConnection(MySql.ConnectionString);
        }

        public async Task<List<Server>> GetAllServersAsync()
        {
            var db = DbConnection();

            const string sql = @"SELECT id, ip_addr, name, last_updated, is_open_mp, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac FROM servers";

            try
            {
                return (await db.QueryAsync<Server>(sql)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[db_err] Could not get servers from database \n {ex}");
                return new List<Server>();
            }
        }

        public async Task<int> GetServerId(string ipAddr)
        {
            var db = DbConnection();

            var sql = @"SELECT id FROM servers WHERE ip_addr=@IpAddr";

            try
            {
                return (await db.QueryAsync<int>(sql, new { IpAddr = ipAddr })).Single();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[db_err] Failed to get ID for {ipAddr} !! \n {ex}");
                return 0;
            }
        }

        public async Task<bool> InsertServer(Server server)
        {
            var db = DbConnection();

            var sql = @"INSERT INTO servers (ip_addr, name, last_updated, is_open_mp, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac)
                        VALUES(@IpAddr, @Name, @LastUpdated, @IsOpenMp, @LagComp, @MapName, @GameMode, @PlayersOnline, @MaxPlayers, @Website, @Version, @Language, @SampCac)";

            try
            {
                return (await db.ExecuteAsync(sql, new
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
            catch (Exception ex)
            {
                Console.WriteLine($"[db_err] Failed to add {server.IpAddr} to the database \n {ex}");
                return false;
            }
        }

        public async Task<bool> UpdateServer(Server server, MySqlConnection? db = null)
        {
            db ??= DbConnection();

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

            if (Global.IsDevelopment) return success;
            
            sql = @"INSERT INTO metrics_server (server_id, players) VALUES (@Id, @PlayersOnline)";

            await db.ExecuteAsync(sql, new { server.Id, server.PlayersOnline });

            return success;
        }
    }
}
