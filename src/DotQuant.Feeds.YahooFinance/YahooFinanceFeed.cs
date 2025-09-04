using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace DotQuant.Feeds.YahooFinance;

/// <summary>
/// A feed that streams Yahoo Finance data (live or historical) into DotQuant's event pipeline.
/// </summary>
public class YahooFinanceFeed : LiveFeed
{
    private static readonly Currency EUR = Currency.GetInstance("EUR");
    public static readonly string Source = "yahoo";

    private readonly IDataReader _dataReader;
    private readonly ILogger<YahooFinanceFeed> _logger;
    private readonly Symbol[] _symbols;
    private readonly DateTime _start;
    private readonly DateTime _end;
    private readonly bool _isLiveMode;
    private readonly TimeSpan _pollingInterval;
    private readonly TimeSpan _timeSpan;
    private readonly IMarketStatusService _marketStatusService;

    public YahooFinanceFeed(
        IDataReader dataReader,
        ILogger<YahooFinanceFeed> logger,
        Symbol[] symbols,
        DateTime start,
        DateTime end,
        IMarketStatusService marketStatusService,
        bool isLiveMode = false,
        TimeSpan? pollingInterval = null)
    {
        _dataReader = dataReader ?? throw new ArgumentNullException(nameof(dataReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
        _start = start;
        _end = end;
        _marketStatusService = marketStatusService ?? throw new ArgumentNullException(nameof(marketStatusService));
        _isLiveMode = isLiveMode;
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(10);
        _timeSpan = _pollingInterval;
    }

    public override async Task PlayAsync(ChannelWriter<Event> channel, CancellationToken ct = default)
    {
        try
        {
            if (_isLiveMode)
            {
                while (!ct.IsCancellationRequested)
                {
                    foreach (var symbol in _symbols)
                    {
                        if (!await _marketStatusService.IsMarketOpenAsync(symbol, ct))
                        {
                            _logger.LogInformation("Market for {Symbol} is closed. Skipping tick.", symbol.ToString());
                            continue;
                        }

                        if (_dataReader.TryGetLatestPrice(symbol, out var latestPrice))
                        {
                            var asset = new Stock(symbol, EUR);
                            var priceItem = new PriceItem(
                                asset,
                                latestPrice.Open,
                                latestPrice.High,
                                latestPrice.Low,
                                latestPrice.Close,
                                latestPrice.Volume,
                                _pollingInterval
                            );

                            var evt = new Event(latestPrice.Date, new List<PriceItem> { priceItem });
                            await channel.WriteAsync(evt, ct);
                            _logger.LogInformation("Live tick for {Symbol} at {Time}: {Close}", symbol.ToString(), latestPrice.Date, priceItem.Close);
                        }
                        else
                        {
                            _logger.LogDebug("No live data for {Symbol} at {Now}", symbol.ToString(), DateTime.UtcNow);
                        }
                    }

                    await Task.Delay(_pollingInterval, ct);
                }
            }
            else
            {
                foreach (var symbol in _symbols)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!_dataReader.TryGetPrices(symbol, _start, _end, out var prices))
                    {
                        _logger.LogWarning("No historical data for {Symbol} from {Start} to {End}", symbol.ToString(), _start, _end);
                        continue;
                    }

                    var asset = new Stock(symbol, EUR);
                    foreach (var price in prices)
                    {
                        ct.ThrowIfCancellationRequested();

                        var priceItem = new PriceItem(asset, price.Open, price.High, price.Low, price.Close, price.Volume, _timeSpan);
                        var evt = new Event(price.Date, new List<PriceItem> { priceItem });

                        await channel.WriteAsync(evt, ct);
                        _logger.LogDebug("Backtest tick for {Symbol} at {Time}", symbol.ToString(), price.Date);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("YahooFinanceFeed playback cancelled.");
        }
        finally
        {
            if (!_isLiveMode)
            {
                channel.TryComplete(); // Only complete for backtest mode
            }
        }
    }
}
