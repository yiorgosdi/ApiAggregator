using Aggregator.Connectors.Abstractions;
using Aggregator.Core;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace Aggregator.Connectors;

public sealed class OpenMeteoConnector : IOpenMeteoConnector
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenMeteoConnector> _logger;

    public OpenMeteoConnector(HttpClient http, ILogger<OpenMeteoConnector> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ConnectorResult<WeatherInfo>> GetAsync(double lat, double lon, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var url = $"forecast?latitude={lat.ToString(CultureInfo.InvariantCulture)}" +
                      $"&longitude={lon.ToString(CultureInfo.InvariantCulture)}" +
                      "&current=temperature_2m,wind_speed_10m&timezone=auto";

            using var resp = await _http.GetAsync(url, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var code = resp.StatusCode;

            _logger.LogInformation("OpenMeteo {Status} {CT} {Preview}",
                (int)code, resp.Content.Headers.ContentType?.MediaType,
                body.Length > 200 ? body[..200] + "..." : body);

            if (!resp.IsSuccessStatusCode)
                return ConnectorResult.Fail<WeatherInfo>("open-meteo", $"HTTP {(int)code}", code, sw.Elapsed);

            var model = JsonSerializer.Deserialize<OpenMeteoResp>(
                body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Empty JSON");

            var info = new WeatherInfo(
                Source: "open-meteo",
                Time: model.current.time,
                Interval: model.current.interval,
                TempC: model.current.temperature_2m,
                WindKph: model.current.wind_speed_10m // το Open-Meteo δίνει km/h
            );

            return ConnectorResult.Ok("open-meteo", info, code, duration: sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenMeteo failed");
            return ConnectorResult.Fail<WeatherInfo>("open-meteo", ex.Message, duration: sw.Elapsed);
        }
    }

    private sealed class OpenMeteoResp
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public CurrentUnits? current_units { get; set; }
        public Current current { get; set; } = new();
        public string? timezone { get; set; } // <-- string, όχι TimeZone
    }

    private sealed class CurrentUnits
    {
        public string? time { get; set; }
        public string? interval { get; set; }
        public string? temperature_2m { get; set; }
        public string? wind_speed_10m { get; set; }
    }

    private sealed class Current
    {
        public string? time { get; set; }
        public int? interval { get; set; }
        public double? temperature_2m { get; set; }
        public double? wind_speed_10m { get; set; }
    }
}