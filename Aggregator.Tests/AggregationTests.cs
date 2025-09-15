using Aggregator.Core;
using Aggregator.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

public class AggregationTests
{
    [Fact]
    public async Task Aggregate_Merges_Sorts_And_Limits()
    {
        var now = DateTimeOffset.UtcNow;
        var hnItem = new ConnectorItem("hn-1", "hn-1", null, null, null, now.AddMinutes(-1));
        var ghItem = new ConnectorItem("gh-1", "gh-1", null, null, null, now);

        var hn = new Mock<IApiConnector>();
        hn.Setup(x => x.Name).Returns("hn");
        hn.Setup(x => x.FetchAsync(It.IsAny<AggregateQuery>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConnectorResultFlatFactory.OkItems("hn", new[] { hnItem }));

        var gh = new Mock<IApiConnector>();
        gh.Setup(x => x.Name).Returns("gh");
        gh.Setup(x => x.FetchAsync(It.IsAny<AggregateQuery>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(ConnectorResultFlatFactory.OkItems("gh", new[] { ghItem }));

        var stats = new Mock<IStats>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        var svc = new AggregateService(new[] { hn.Object, gh.Object }, stats.Object, cache);

        var result = await svc.GetAsync(new AggregateQuery { Take = 1 }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.Single().Title.Should().Be("gh-1"); // πιο πρόσφατο
        hn.Verify(x => x.FetchAsync(It.IsAny<AggregateQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        gh.Verify(x => x.FetchAsync(It.IsAny<AggregateQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}