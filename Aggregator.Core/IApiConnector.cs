namespace Aggregator.Core;

public interface IApiConnector
{
    string Name { get; }
    Task<ConnectorResultFlat> FetchAsync(AggregateQuery query, CancellationToken ct);
}