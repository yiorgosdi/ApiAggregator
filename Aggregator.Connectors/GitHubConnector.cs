using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Aggregator.Core;

namespace Aggregator.Connectors;

public sealed class GitHubConnector : IGitHubConnector
{
    private readonly HttpClient _http;

    public GitHubConnector(HttpClient http) => _http = http;

    public async Task<ConnectorResult<List<ConnectorItem>>> SearchAsync(string query, int limit, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var url =
                $"/search/repositories?q={Uri.EscapeDataString(query)}&sort=stars&order=desc&per_page={Math.Clamp(limit, 1, 50)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var resp = await _http.SendAsync(req, ct);
            var code = resp.StatusCode;
            resp.EnsureSuccessStatusCode();

            var r = await resp.Content.ReadFromJsonAsync<GithubResp>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            var items = (r?.items ?? [])
                .Select(x => new ConnectorItem(
                    Source: "github",
                    Title: x.full_name ?? "(repo)",
                    Url: x.html_url,
                    Stars: x.stargazers_count,
                    Points: null,
                    CreatedAt: x.created_at
                ))
                .ToList();

            return ConnectorResult.Ok("github", items, code, duration: sw.Elapsed);
        }
        catch (Exception ex)
        {
            return ConnectorResult.Fail<List<ConnectorItem>>("github", ex.Message, duration: sw.Elapsed);
        }
    }

    private sealed class GithubResp
    {
        public List<Item> items { get; set; } = new();
    }

    private sealed class Item
    {
        public string? full_name { get; set; }
        public string? html_url { get; set; }
        public int? stargazers_count { get; set; }
        public DateTimeOffset? created_at { get; set; }
    }
}