namespace Aggregator.Core;

public record AggregateResult(
    string? Query,
    WeatherInfo? Weather,
    IReadOnlyList<ConnectorItem> Items,
    bool Partial,
    string[]? Errors,
    bool FromCache);

public static class AggregateMerger
{
    public static AggregateResult Merge(AggregateQuery q, IEnumerable<ConnectorResultFlat> results)
    {
        var items = new List<ConnectorItem>();
        WeatherInfo? weather = null;
        var errors = new List<string>();
        var partial = false;

        foreach (var r in results)
        {
            if (!r.Success)
            {
                partial = true;
                if (!string.IsNullOrWhiteSpace(r.Error))
                    errors.Add($"{r.Source}: {r.Error}");
                continue;
            }

            if (r.Items is { } it) items.AddRange(it);
            if (r.Weather is { } w) weather ??= w;
        }

        // Filters
        if (!string.IsNullOrEmpty(q.Source))
        {
            var allow = q.Source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(s => s.ToLowerInvariant()).ToHashSet();
            items = items.Where(i => allow.Contains(i.Source.ToLowerInvariant())).ToList();
        }

        if (q.MinStars is > 0)
            items = items.Where(i => (i.Stars ?? 0) >= q.MinStars).ToList();

        if (q.MinPoints is > 0)
            items = items.Where(i => (i.Points ?? 0) >= q.MinPoints).ToList();

        if (q.From is not null)
            items = items.Where(i => (i.CreatedAt ?? DateTimeOffset.MinValue) >= q.From).ToList();

        if (q.To is not null)
            items = items.Where(i => (i.CreatedAt ?? DateTimeOffset.MaxValue) <= q.To).ToList();


        // Sorts
        items = (q.Sort?.ToLowerInvariant()) switch
        {
            "popularity" => items.OrderByDescending(i => Math.Max(i.Stars ?? 0, i.Points ?? 0)).ToList(),
            _ => items.OrderByDescending(i => i.CreatedAt ?? DateTimeOffset.MinValue).ToList(),
        };

        // Take
        var take = Math.Clamp(q.Take ?? 50, 1, 200);
        items = items.Take(take).ToList();

        return new AggregateResult(
            q.Query,
            weather,
            items,
            partial,
            errors.Count == 0 ? null : errors.ToArray(),
            FromCache: false
        );
    }
}