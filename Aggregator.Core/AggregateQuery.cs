namespace Aggregator.Core;

public sealed class AggregateQuery
{
    public string? Query { get; set; }
    public string? Source { get; set; }   // comma-separated filter ("meteo,github,hn")
    public int? Take { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // προαιρετικά:
    public int? MinStars { get; set; }
    public int? MinPoints { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Sort { get; set; }     // "popularity" | default: by CreatedAt desc
} 