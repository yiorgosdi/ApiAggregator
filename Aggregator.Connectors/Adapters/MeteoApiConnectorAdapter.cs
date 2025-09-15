using Aggregator.Connectors.Abstractions;
using Aggregator.Core;

namespace Aggregator.Connectors;

public sealed class MeteoApiConnectorAdapter : IApiConnector
{
    private readonly IOpenMeteoConnector _meteo;
    public string Name => "meteo";

    public MeteoApiConnectorAdapter(IOpenMeteoConnector meteo) => _meteo = meteo;

    public async Task<ConnectorResultFlat> FetchAsync(AggregateQuery q, CancellationToken ct)
    {
        if (q.Latitude is null || q.Longitude is null)
            return ConnectorResultFlatFactory.Fail(Name, "missing latitude/longitude");

        var res = await _meteo.GetAsync(q.Latitude.Value, q.Longitude.Value, ct);
        return res.Success
            ? ConnectorResultFlatFactory.OkWeather(Name,
                new WeatherInfo("open-meteo", res.Data!.Time, res.Data!.Interval, res.Data!.TempC, res.Data!.WindKph))
            : ConnectorResultFlatFactory.Fail(Name, res.Error ?? "unknown error", res.StatusCode);
    }
}