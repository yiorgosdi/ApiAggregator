using Aggregator.Core;

namespace Aggregator.Infrastructure;

public static class CacheKeys
{
    public static string ForAggregate(AggregateQuery q)
        => $"agg:v1:q={q.Query}|lat={q.Latitude}|lon={q.Longitude}|src={q.Source}|sort={q.Sort}|ms={q.MinStars}|mp={q.MinPoints}|from={q.From}|to={q.To}|take={q.Take}";
}
