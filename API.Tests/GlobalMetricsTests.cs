using SAMonitor.Data;
using Xunit;

namespace SAMonitor.Tests;

public sealed class GlobalMetricsTests
{
    [Theory]
    [InlineData(750)]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(1001)]
    [InlineData(1499)]
    public void Trimming_LimitsLargeResponsesTo500Points(int count)
    {
        var metrics = CreateMetrics(count);

        var result = StatsManager.GetGlobalMetrics(6, false, metrics);

        Assert.InRange(result.Count, 1, 500);
        Assert.Equal(metrics[0].Time, result[0].Time);
        Assert.True(result[0].Time > result[^1].Time);
    }

    [Fact]
    public void Trimming_PreservesGroupAveragesAndFirstTimestamp()
    {
        var metrics = CreateMetrics(750);

        var result = StatsManager.GetGlobalMetrics(6, false, metrics);

        Assert.Equal(375, result.Count);
        Assert.Equal(0, result[0].Players);
        Assert.Equal(1, result[0].Servers);
        Assert.Equal(1, result[0].OmpServers);
        Assert.Equal(metrics[0].Time, result[0].Time);
        Assert.Equal(748, result[^1].Players);
        Assert.Equal(metrics[748].Time, result[^1].Time);
    }

    [Theory]
    [InlineData(749, false)]
    [InlineData(1001, true)]
    public void UntrimmedResponses_PreserveSamplesAndExcludeOldMetrics(int count, bool skipTrimming)
    {
        var metrics = CreateMetrics(count);
        var old = new GlobalMetrics(1, 2, 3, DateTime.UtcNow.AddHours(-7));

        var result = StatsManager.GetGlobalMetrics(6, skipTrimming, metrics.Append(old));

        Assert.Equal(metrics, result);
    }

    private static List<GlobalMetrics> CreateMetrics(int count)
    {
        var time = DateTime.UtcNow;
        return Enumerable.Range(0, count)
            .Select(index => new GlobalMetrics(index, index * 2, index * 3, time.AddSeconds(-index)))
            .ToList();
    }
}
