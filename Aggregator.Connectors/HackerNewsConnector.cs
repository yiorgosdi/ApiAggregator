using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Core;
using Microsoft.Extensions.Logging;

namespace Aggregator.Connectors;

public sealed class HackerNewsConnector : IHackerNewsConnector
{
    private readonly HttpClient _http;
    private readonly ILogger<HackerNewsConnector> _logger;

    public HackerNewsConnector(HttpClient http, ILogger<HackerNewsConnector> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ConnectorResult<List<ConnectorItem>>> SearchAsync(string query, int limit, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var url =
                $"https://hn.algolia.com/api/v1/search?query={Uri.EscapeDataString(query)}&hitsPerPage={Math.Clamp(limit, 1, 50)}";

            var r = await _http.GetFromJsonAsync<HnResp>(
                url,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            var items = (r?.hits ?? [])
                .Select(h => new ConnectorItem(
                    Source: "hn",
                    Title: h.title ?? "(no title)",
                    Url: h.url,
                    Stars: null,
                    Points: h.points,
                    CreatedAt: h.created_at_i is > 0 ? DateTimeOffset.FromUnixTimeSeconds(h.created_at_i.Value) : null
                ))
                .ToList();

            return ConnectorResult.Ok("hn", items, duration: sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HackerNews failed for query '{Query}'", query);
            return ConnectorResult.Fail<List<ConnectorItem>>("hn", ex.Message, duration: sw.Elapsed);
        }
    }

    private sealed class HnResp
    {
        public List<Hit> hits { get; set; } = new();
    }

    private sealed class Hit
    {
        public string? title { get; set; }
        public string? url { get; set; }
        public int? points { get; set; }
        public long? created_at_i { get; set; }
    }
}