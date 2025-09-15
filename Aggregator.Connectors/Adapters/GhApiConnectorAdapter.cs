using Aggregator.Core;

namespace Aggregator.Connectors;

public sealed class GhApiConnectorAdapter : IApiConnector
{
    private readonly IGitHubConnector _gh;
    public string Name => "github";

    public GhApiConnectorAdapter(IGitHubConnector gh) => _gh = gh;

    public async Task<ConnectorResultFlat> FetchAsync(AggregateQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.Query))
            return ConnectorResultFlatFactory.Fail(Name, "missing query");

        var res = await _gh.SearchAsync(q.Query!, Math.Clamp(q.Take ?? 50, 1, 50), ct);
        return res.Success
            ? ConnectorResultFlatFactory.OkItems(Name, res.Data!)
            : ConnectorResultFlatFactory.Fail(Name, res.Error ?? "unknown error", res.StatusCode);
    }
}