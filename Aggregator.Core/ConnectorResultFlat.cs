using System.Net;

namespace Aggregator.Core;

public record ConnectorResultFlat(
    string Source,
    bool Success,
    IReadOnlyList<ConnectorItem>? Items = null,
    WeatherInfo? Weather = null,
    string? Error = null,
    HttpStatusCode? StatusCode = null,
    bool FromCache = false,
    TimeSpan? Duration = null
);

public static class ConnectorResultFlatFactory
{
    public static ConnectorResultFlat OkItems(
        string source,
        IEnumerable<ConnectorItem> items,
        HttpStatusCode? code = null,
        bool fromCache = false,
        TimeSpan? duration = null)
        => new(source, true, items.ToList(), null, null, code, fromCache, duration);

    public static ConnectorResultFlat OkWeather(
        string source,
        WeatherInfo weather,
        HttpStatusCode? code = null,
        bool fromCache = false,
        TimeSpan? duration = null)
        => new(source, true, null, weather, null, code, fromCache, duration);

    public static ConnectorResultFlat Fail(
        string source,
        string error,
        HttpStatusCode? code = null,
        TimeSpan? duration = null)
        => new(source, false, null, null, error, code, false, duration);
}