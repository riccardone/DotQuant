using System.Threading.Channels;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Feeds.EodHistoricalData;
using DotQuant.Feeds.YahooFinance;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.LiveFeedWithFallBack;

/// <summary>
/// A live feed that attempts to use EODHistoricalDataFeed first,
/// and falls back to YahooFinanceFeed if that fails.
/// </summary>
public class FallbackFeed : IFeed
{
    private readonly YahooFinanceFeed _yahooFinanceFeed;
    private readonly EodHistoricalDataFeed _eodHistoricalDataFeed;
    private readonly ILogger<FallbackFeed> _logger;

    public FallbackFeed(YahooFinanceFeed yahoo, EodHistoricalDataFeed eod, ILogger<FallbackFeed> logger)
    {
        _logger = logger;
        _yahooFinanceFeed = yahoo;
        _eodHistoricalDataFeed = eod;
    }

    public async Task PlayAsync(ChannelWriter<Event> channel, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Attempting primary feed (EOD Historical Data)");
            await _eodHistoricalDataFeed.PlayAsync(channel, ct);
        }
        catch (HttpRequestException ex) when ((int?)ex.StatusCode == 404)
        {
            _logger.LogWarning("Primary feed returned 404. Falling back to Yahoo Finance.");
            await _yahooFinanceFeed.PlayAsync(channel, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure in EOD feed. Falling back to Yahoo.");
            await _yahooFinanceFeed.PlayAsync(channel, ct);
        }
    }
}
