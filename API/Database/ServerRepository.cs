using SAMonitor.Data;
using SAMonitor.Utils;
using static Dapper.SqlMapper;

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

    public static async Task<bool> InsertServer(Server server)
    {
        using var db = await DatabasePool.GetConnectionAsync();

        const string sql = """
                           INSERT INTO servers (ip_addr, name, last_updated, is_open_mp, lag_comp, map_name, gamemode, players_online, max_players, website, version, language, sampcac, weather)
                           VALUES(@IpAddr, @Name, @LastUpdated, @IsOpenMp, @LagComp, @MapName, @GameMode, @PlayersOnline, @MaxPlayers, @Website, @Version, @Language, @SampCac, @Weather)
                           """;

        try
        {
            return await db.ExecuteAsync(sql, new
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
            await Helpers.LogError($"InsertServer {server.IpAddr}", ex);
            return false;
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
}
