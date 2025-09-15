using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Aggregator.Infrastructure;

public static class Resilience
{
    public static IAsyncPolicy<HttpResponseMessage> ForHttp() =>
        Policy.WrapAsync(Timeout(), Retry(), Breaker());

    private static IAsyncPolicy<HttpResponseMessage> Timeout() =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2));

    private static IAsyncPolicy<HttpResponseMessage> Retry() =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(2, i => TimeSpan.FromMilliseconds(200 * i));

    private static IAsyncPolicy<HttpResponseMessage> Breaker() =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 2,
                                 durationOfBreak: TimeSpan.FromSeconds(10));
}
