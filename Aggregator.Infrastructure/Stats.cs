using System.Collections.Concurrent;

namespace Aggregator.Infrastructure;

public sealed class ApiStats
{
    private long _total;
    private long _errors;
    private long _sumMs;
    private long _lt100;
    private long _lt250;
    private long _gt250;

    public void Observe(long ms, bool success)
    {
        Interlocked.Increment(ref _total);
        if (!success) Interlocked.Increment(ref _errors);
        Interlocked.Add(ref _sumMs, ms);
        if (ms < 100) Interlocked.Increment(ref _lt100);
        else if (ms < 250) Interlocked.Increment(ref _lt250);
        else Interlocked.Increment(ref _gt250);
    }

    public ApiStatsSnapshot Snapshot(string api)
        => new(api,
               Interlocked.Read(ref _total),
               Interlocked.Read(ref _errors),
               totalMs: Interlocked.Read(ref _sumMs),
               lt100: Interlocked.Read(ref _lt100),
               lt250: Interlocked.Read(ref _lt250),
               gt250: Interlocked.Read(ref _gt250));
}

public record ApiStatsSnapshot(string Api, long Total, long Errors, long totalMs, long lt100, long lt250, long gt250)
{
    public double AvgMs => Total == 0 ? 0 : (double)totalMs / Total;
    public object Buckets => new { _lt100 = lt100, _100_249 = lt250, _ge250 = gt250 };
}

public interface IStats
{
    void Observe(string api, long ms, bool success);
    IReadOnlyList<ApiStatsSnapshot> Snapshot();
}

public sealed class InMemoryStats : IStats
{
    private readonly ConcurrentDictionary<string, ApiStats> _byApi = new();

    public void Observe(string api, long ms, bool success)
        => _byApi.GetOrAdd(api, _ => new ApiStats()).Observe(ms, success);

    public IReadOnlyList<ApiStatsSnapshot> Snapshot()
        => _byApi.Select(kv => kv.Value.Snapshot(kv.Key)).OrderBy(x => x.Api).ToList();
}
