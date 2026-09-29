using Dapper;
using SAMonitor.Data;
using SAMonitor.Utils;
using System.Data.Common;

namespace SAMonitor.Database;

public static class ServerRepository
{
    public static async Task<List<Server>> GetAllServersAsync()
    {
        using var db = await DatabasePool.GetConnectionAsync();

        const string sql = "SELECT id, ip_addr, name, last_updated, is_open_mp, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac, sponsor_until, weather FROM servers";

        try
        {
            return (await db.QueryAsync<Server>(sql)).ToList();
        }
        catch (Exception ex)
        {
            await Helpers.LogError("GetAllServersAsync", ex);
            return [];
        }
    }

    public static async Task<int> GetServerId(string ipAddr)
    {
        using var db = await DatabasePool.GetConnectionAsync();

        const string sql = "SELECT id FROM servers WHERE ip_addr=@IpAddr";

        try
        {
            return (await db.QueryAsync<int>(sql, new { IpAddr = ipAddr })).Single();
        }
        catch (Exception ex)
        {
            await Helpers.LogError($"GetServerId {ipAddr}", ex);
            return 0;
        }
    }

    public static async Task<int?> InsertServer(Server server, IReadOnlyCollection<int> replacedIds)
    {
        try
        {
            using var db = await DatabasePool.GetConnectionAsync();
            return await InsertServer(db, server, replacedIds);
        }
        catch (Exception ex)
        {
            await Helpers.LogError($"InsertServer {server.IpAddr}", ex);
            return null;
        }
    }

    internal static async Task<int> InsertServer(DbConnection db, Server server, IReadOnlyCollection<int> replacedIds)
    {
        const string sql = """
                           INSERT INTO servers (ip_addr, name, last_updated, is_open_mp, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac, weather)
                           VALUES(@IpAddr, @Name, @LastUpdated, @IsOpenMp, @LagComp, @MapName, @GameMode, @PlayersOnline, @MaxPlayers, @Website, @Version, @Language, @SampCac, @Weather)
                           """;

        using var tx = await db.BeginTransactionAsync();
        try
        {
            if (replacedIds.Count > 0)
            {
                await db.ExecuteAsync("DELETE FROM servers WHERE id IN @Ids", new { Ids = replacedIds }, tx);
            }

            if (await db.ExecuteAsync(sql, server, tx) != 1)
            {
                throw new InvalidOperationException("Server insertion did not create a row.");
            }

            int id = await db.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID()", transaction: tx);
            if (id <= 0)
            {
                throw new InvalidOperationException("Server insertion did not return a valid ID.");
            }

            await tx.CommitAsync();
            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public static async Task InsertServerMetrics(int serverId, int playerAmount)
    {
        using var db = await DatabasePool.GetConnectionAsync();

        const string sql = "INSERT INTO metrics_server (server_id, players) VALUES (@server_id, @player_amount)";

        await db.ExecuteAsync(sql, new { server_id = serverId, player_amount = playerAmount });
    }

    public static async Task<bool> UpdateServer(Server server)
    {
        using var db = await DatabasePool.GetConnectionAsync();

        string sql = """
                     UPDATE servers
                     SET name=@Name, last_updated=@LastUpdated, is_open_mp=@IsOpenMp, lag_comp=@LagComp, map_name=@MapName, gamemode=@GameMode, players_online=@PlayersOnline, max_players=@MaxPlayers, website=@Website, version=@Version, language=@Language, sampcac=@SampCac, weather=@Weather
                     WHERE ip_addr = @IpAddr
                     """;

        bool success;

        try
        {
            success = await db.ExecuteAsync(sql, new
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
                server.SampCac,
                server.Weather
            }) > 0;
        }
        catch (Exception ex)
        {
            await Helpers.LogError($"UpdateServer {server.IpAddr}", ex);
            success = false;
        }

        // then add a metric entry. ONLY IF IN PRODUCTION.

        if (Helpers.IsDevelopment)
        {
            return success;
        }

        sql = "INSERT INTO metrics_server (server_id, players) VALUES (@Id, @PlayersOnline)";

        await db.ExecuteAsync(sql, new { server.Id, server.PlayersOnline });

        return success;
    }

    internal static async Task UpdateServersBatch(IReadOnlyList<ServerUpdate> updates)
    {
        if (updates.Count == 0) return;

        using var db = await DatabasePool.GetConnectionAsync();
        await UpdateServersBatch(db, updates, !Helpers.IsDevelopment);
    }

    internal static async Task UpdateServersBatch(DbConnection db, IReadOnlyList<ServerUpdate> updates, bool recordMetrics)
    {
        if (updates.Count == 0) return;

        var ids = updates.Where(x => x.Id > 0).Select(x => x.Id).Distinct().ToArray();
        if (ids.Length == 0) return;

        const string updateSql = """
                                 UPDATE servers
                                 SET name=@Name, last_updated=@LastUpdated, is_open_mp=@IsOpenMp, lag_comp=@LagComp, map_name=@MapName, gamemode=@GameMode, players_online=@PlayersOnline, max_players=@MaxPlayers, website=@Website, version=@Version, language=@Language, sampcac=@SampCac, weather=@Weather
                                 WHERE id = @Id
                                 """;

        using var tx = await db.BeginTransactionAsync();

        try
        {
            var existingIds = (await db.QueryAsync<int>(
                "SELECT id FROM servers WHERE id IN @Ids FOR UPDATE", new { Ids = ids }, tx)).ToHashSet();
            var activeUpdates = updates.Where(x => existingIds.Contains(x.Id)).ToList();

            foreach (var server in activeUpdates.GroupBy(x => x.Id).Select(x => x.Last()))
            {
                await db.ExecuteAsync(updateSql, server, tx);
            }

            if (recordMetrics && activeUpdates.Count > 0)
            {
                // Dapper doesn't expand collection parameters for arbitrary SQL
                var sb = new System.Text.StringBuilder(
                    "INSERT INTO metrics_server (server_id, players, time) VALUES ");
                var dynParams = new DynamicParameters();
                for (int i = 0; i < activeUpdates.Count; i++)
                {
                    var update = activeUpdates[i];
                    if (i > 0) sb.Append(',');
                    sb.Append($"(@id{i}, @players{i}, @time{i})");
                    dynParams.Add($"id{i}", update.Id);
                    dynParams.Add($"players{i}", update.PlayersOnline);
                    dynParams.Add($"time{i}", update.LastUpdated);
                }

                await db.ExecuteAsync(sb.ToString(), dynParams, tx);
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public static async Task<List<ServerMetrics>> GetServerMetrics(int id, DateTime requestTime, int includeMisses = 0)
    {
        using var db = await DatabasePool.GetConnectionAsync();

        string sql;

        if (includeMisses > 0)
        {
            sql = "SELECT players, time FROM metrics_server WHERE time > @requestTime AND server_id = @id ORDER BY time DESC";
        }
        else
        {
            sql = "SELECT players, time FROM metrics_server WHERE time > @requestTime AND server_id = @id AND players >= 0 ORDER BY time DESC";
        }

        return (await db.QueryAsync<ServerMetrics>(sql, new { requestTime, id })).ToList();
    }

    public static async Task<bool> DeleteServer(int id)
    {
        using var db = await DatabasePool.GetConnectionAsync();

        const string sql = "DELETE FROM servers WHERE id = @Id";

        try
        {
            return await db.ExecuteAsync(sql, new { Id = id }) > 0;
        }
        catch (Exception ex)
        {
            await Helpers.LogError($"DeleteServer {id}", ex);
            return false;
        }
    }
}
