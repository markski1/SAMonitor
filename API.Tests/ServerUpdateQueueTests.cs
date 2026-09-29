using SAMonitor.Data;
using SAMonitor.Utils;
using Xunit;

namespace SAMonitor.Tests;

public sealed class ServerUpdateQueueTests
{
    [Fact]
    public async Task FailedBatch_PreservesSnapshotsAndNewArrivals()
    {
        var persistence = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        List<ServerUpdate[]> attempts = [];
        var queue = new ServerUpdateQueue(batch =>
        {
            attempts.Add(batch.ToArray());
            return attempts.Count == 1 ? persistence.Task : Task.CompletedTask;
        });
        using var first = new Server("203.0.113.1:7777") { Id = 1, PlayersOnline = 10 };
        using var second = new Server("203.0.113.2:7777") { Id = 2 };
        var measuredAt = first.LastUpdated;
        queue.Enqueue(first);
        var processing = queue.ProcessAsync();
        first.PlayersOnline = 99;
        first.LastUpdated = measuredAt.AddHours(1);
        queue.Enqueue(second);
        persistence.SetException(new IOException("Database unavailable"));

        await Assert.ThrowsAsync<IOException>(() => processing);
        await queue.ProcessAsync();
        await queue.ProcessAsync();

        Assert.Equal(2, attempts.Count);
        Assert.Equal(new[] { 1, 2 }, attempts[1].Select(x => x.Id));
        Assert.Equal(10, attempts[1][0].PlayersOnline);
        Assert.Equal(measuredAt, attempts[1][0].LastUpdated);
    }

    [Fact]
    public async Task ConcurrentProcessing_PreservesOrder()
    {
        var persistence = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        List<int> saved = [];
        var queue = new ServerUpdateQueue(batch =>
        {
            saved.AddRange(batch.Select(x => x.PlayersOnline));
            return saved.Count == 1 ? persistence.Task : Task.CompletedTask;
        });
        using var server = new Server("203.0.113.1:7777") { Id = 1, PlayersOnline = 10 };
        queue.Enqueue(server);
        var first = queue.ProcessAsync();
        server.PlayersOnline = 20;
        queue.Enqueue(server);
        var second = queue.ProcessAsync();

        Assert.False(second.IsCompleted);
        persistence.SetResult();
        await Task.WhenAll(first, second);
        Assert.Equal(new[] { 10, 20 }, saved);
    }

    [Fact]
    public async Task PartialFailure_RetriesOnlyUnsavedMeasurements()
    {
        int attempts = 0;
        List<int> saved = [];
        var queue = new ServerUpdateQueue(batch =>
        {
            if (++attempts == 2) return Task.FromException(new IOException("Database unavailable"));
            saved.AddRange(batch.Select(x => x.PlayersOnline));
            return Task.CompletedTask;
        });
        using var server = new Server("203.0.113.1:7777") { Id = 1 };
        for (int i = 0; i < 600; i++)
        {
            server.PlayersOnline = i;
            queue.Enqueue(server);
        }

        await Assert.ThrowsAsync<IOException>(queue.ProcessAsync);
        await queue.ProcessAsync();
        await queue.ProcessAsync();

        Assert.Equal(Enumerable.Range(0, 600), saved);
    }
}
