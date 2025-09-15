using Aggregator.Core;

namespace Aggregator.Connectors;

public interface IHackerNewsConnector
{
    Task<ConnectorResult<List<ConnectorItem>>>  SearchAsync(string query, int limit, CancellationToken ct);
}
