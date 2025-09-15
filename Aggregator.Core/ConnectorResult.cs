using System.Net;

namespace Aggregator.Core;

public record ConnectorItem(
    string Source, string Title, string? Url,
    int? Stars, int? Points, DateTimeOffset? CreatedAt);

public record WeatherInfo(string Source, string? Time, int? Interval, double? TempC, double? WindKph);

public record ConnectorResult<T>(
    string Source,
    bool Success,
    T? Data,
    string? Error = null,
    HttpStatusCode? StatusCode = null,
    bool FromCache = false,
    TimeSpan? Duration = null
);

public static class ConnectorResult
{
    public static ConnectorResult<T> Ok<T>(
        string source, T data,
        HttpStatusCode? code = null,
        bool fromCache = false,
        TimeSpan? duration = null)
        => new(source, true, data, null, code, fromCache, duration);

    public static ConnectorResult<T> Fail<T>(
        string source, string error,
        HttpStatusCode? code = null,
        TimeSpan? duration = null)
        => new(source, false, default, error, code, false, duration);
}