using System.Data;
using SAMonitor.Data;
using SAMonitor.Database;
using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ServerRepositoryTransactionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacement_CommitsTogetherOrRollsBack(bool insertFails)
    {
        using var db = new DbConnectionStub(command =>
        {
            if (insertFails && command.Sql.StartsWith("INSERT")) throw new IOException("Insert failed");
            return command.Sql.Contains("LAST_INSERT_ID") ? 100 : 1;
        });
        using var server = new Server("203.0.113.1:7777");

        if (insertFails)
            await Assert.ThrowsAsync<IOException>(() => ServerRepository.InsertServer(db, server, [42]));
        else
            Assert.Equal(100, await ServerRepository.InsertServer(db, server, [42]));

        Assert.StartsWith("DELETE FROM servers", db.Commands[0].Sql);
        Assert.StartsWith("INSERT INTO servers", db.Commands[1].Sql);
        var transaction = Assert.Single(db.Transactions);
        Assert.All(db.Commands, command => Assert.Same(transaction, command.Transaction));
        Assert.Equal(!insertFails, transaction.Committed);
        Assert.Equal(insertFails, transaction.RolledBack);
    }

    [Fact]
    public async Task Batch_RetriesMeasurementsAndSkipsDeletedServers()
    {
        bool failMetrics = true;
        using var db = new DbConnectionStub(command =>
        {
            if (command.Sql.StartsWith("SELECT id"))
            {
                var table = new DataTable();
                table.Columns.Add("id", typeof(int));
                table.Rows.Add(1);
                return table.CreateDataReader();
            }
            if (command.Sql.StartsWith("INSERT") && failMetrics) throw new IOException("Metrics insert failed");
            return 1;
        });
        var queue = new ServerUpdateQueue(batch => ServerRepository.UpdateServersBatch(db, batch, true));
        using var active = new Server("203.0.113.1:7777") { Id = 1, PlayersOnline = 10 };
        using var deleted = new Server("203.0.113.2:7777") { Id = 2 };
        var firstTime = active.LastUpdated;
        queue.Enqueue(active);
        active.PlayersOnline = 20;
        active.LastUpdated = firstTime.AddMinutes(20);
        queue.Enqueue(active);
        queue.Enqueue(deleted);

        await Assert.ThrowsAsync<IOException>(queue.ProcessAsync);
        Assert.True(db.Transactions[0].RolledBack);
        active.PlayersOnline = 99;
        failMetrics = false;
        await queue.ProcessAsync();

        Assert.True(db.Transactions[1].Committed);
        var transaction = db.Transactions[1];
        var update = Assert.Single(db.Commands, x => x.Transaction == transaction && x.Sql.StartsWith("UPDATE"));
        Assert.Equal(1, update.Parameters["Id"]);
        Assert.Equal(20, update.Parameters["PlayersOnline"]);
        var metrics = Assert.Single(db.Commands, x => x.Transaction == transaction && x.Sql.StartsWith("INSERT"));
        Assert.Equal(1, metrics.Parameters["id0"]);
        Assert.Equal(1, metrics.Parameters["id1"]);
        Assert.False(metrics.Parameters.ContainsKey("id2"));
        Assert.Equal(10, metrics.Parameters["players0"]);
        Assert.Equal(20, metrics.Parameters["players1"]);
        Assert.Equal(firstTime, metrics.Parameters["time0"]);
        Assert.Equal(firstTime.AddMinutes(20), metrics.Parameters["time1"]);
    }
}
