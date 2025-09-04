using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Feeds.EodHistoricalData;
using DotQuant.Feeds.YahooFinance;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Channels;

namespace DotQuant.Feeds.LiveFeedWithFallBack;

public class FallbackFeed : IFeed
{
    private readonly ILogger _logger;
    private readonly List<IFeed> _feeds;

    public FallbackFeed(ILogger logger, IEnumerable<IFeed> feeds)
    {
        _logger = logger;
        _feeds = feeds.ToList();
    }

    public async Task PlayAsync(ChannelWriter<Event> writer, CancellationToken cancellationToken)
    {
        if (_feeds.Count == 0)
            throw new InvalidOperationException("No feeds provided to fallback wrapper.");

        // Assume all feeds share the same symbol set (for now)
        while (!cancellationToken.IsCancellationRequested)
        {
            var symbols = GetAllSymbols(_feeds.First()); // Helper to extract symbol list

            foreach (var symbol in symbols)
            {
                bool success = false;

                foreach (var feed in _feeds)
                {
                    try
                    {
                        await feed.PlayAsyncForSymbol(writer, cancellationToken, symbol);
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Feed {FeedType} failed for {Symbol}", feed.GetType().Name, symbol);
                    }
                }

                if (!success)
                {
                    _logger.LogError("All feeds failed for symbol {Symbol}", symbol);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); // or configurable
        }
    }

    private static string[] GetAllSymbols(IFeed feed)
    {
        // Assumes feed exposes symbols via reflection or internal prop
        var symbolsField = feed.GetType().GetField("_symbols", BindingFlags.NonPublic | BindingFlags.Instance);
        return symbolsField?.GetValue(feed) as string[] ?? Array.Empty<string>();
    }
}

public static class FeedExtensions
{
    public static async Task PlayAsyncForSymbol(this IFeed feed, ChannelWriter<Event> writer, CancellationToken token, string symbol)
    {
        switch (feed)
        {
            case EodHistoricalDataFeed eod:
                await eod.PlayOneAsync(writer, token, symbol);
                break;

            case YahooFinanceFeed yahoo:
                await yahoo.PlayOneAsync(writer, token, symbol);
                break;

            default:
                throw new NotSupportedException($"Feed {feed.GetType().Name} does not support per-symbol fallback");
        }
    }
}
