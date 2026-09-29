using SAMonitor.Data;

namespace SAMonitor.Utils;

internal sealed class ServerUpdateQueue(Func<IReadOnlyList<ServerUpdate>, Task> persist)
{
    private const int BatchSize = 256;
    private readonly LinkedList<ServerUpdate> _pending = new();
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _processing = new(1);

    internal void Enqueue(Server server)
    {
        var update = new ServerUpdate(server);
        lock (_lock)
        {
            _pending.AddLast(update);
        }
    }

    internal async Task ProcessAsync()
    {
        await _processing.WaitAsync();
        try
        {
            int remaining;
            lock (_lock)
            {
                remaining = _pending.Count;
            }

            while (remaining > 0)
            {
                List<ServerUpdate> batch = [];
                lock (_lock)
                {
                    while (batch.Count < BatchSize && batch.Count < remaining && _pending.First is not null)
                    {
                        batch.Add(_pending.First.Value);
                        _pending.RemoveFirst();
                    }
                }

                try
                {
                    await persist(batch);
                    remaining -= batch.Count;
                }
                catch
                {
                    lock (_lock)
                    {
                        for (int i = batch.Count - 1; i >= 0; i--)
                        {
                            _pending.AddFirst(batch[i]);
                        }
                    }
                    throw;
                }
            }
        }
        finally
        {
            _processing.Release();
        }
    }
}
