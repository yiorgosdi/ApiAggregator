using System.Diagnostics;
using Aggregator.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Aggregator.Infrastructure;

public sealed class AggregateService
{
    private readonly IEnumerable<IApiConnector> _connectors;
    private readonly IStats _stats;
    private readonly IMemoryCache _cache;

    public AggregateService(IEnumerable<IApiConnector> connectors, IStats stats, IMemoryCache cache)
        => (_connectors, _stats, _cache) = (connectors, stats, cache);

    public async Task<AggregateResult> GetAsync(AggregateQuery q, CancellationToken ct)
    {
        var key = CacheKeys.ForAggregate(q);

        if (_cache.TryGetValue(key, out AggregateResult cached))
            return cached with { FromCache = true };

        var tasks = _connectors.Select(c => FetchTrackedAsync(c, q, ct));
        var results = await Task.WhenAll(tasks);

        var merged = AggregateMerger.Merge(q, results);
        _cache.Set(key, merged, TimeSpan.FromSeconds(60));
        return merged;
    }

    private async Task<ConnectorResultFlat> FetchTrackedAsync(IApiConnector c, AggregateQuery q, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var res = await c.FetchAsync(q, ct);
            sw.Stop();
            _stats.Observe(c.Name, sw.ElapsedMilliseconds, success: res.Success);
            // Προσθέτουμε τη διάρκεια για observability
            return res with { Duration = sw.Elapsed };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _stats.Observe(c.Name, sw.ElapsedMilliseconds, success: false);
            return ConnectorResultFlatFactory.Fail(c.Name, ex.Message, duration: sw.Elapsed);
        }
    }
}