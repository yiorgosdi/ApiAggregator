using Aggregator.Core;

namespace Aggregator.Connectors;

public sealed class HnApiConnectorAdapter : IApiConnector
{
    private readonly IHackerNewsConnector _hn;
    public string Name => "hn";

    public HnApiConnectorAdapter(IHackerNewsConnector hn) => _hn = hn;

    public async Task<ConnectorResultFlat> FetchAsync(AggregateQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.Query))
            return ConnectorResultFlatFactory.Fail(Name, "missing query");

        var res = await _hn.SearchAsync(q.Query!, Math.Clamp(q.Take ?? 50, 1, 50), ct);
        return res.Success
            ? ConnectorResultFlatFactory.OkItems(Name, res.Data!)
            : ConnectorResultFlatFactory.Fail(Name, res.Error ?? "unknown error", res.StatusCode);
    }
}