using System.Threading.Channels;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.AlphaVantage
{
    /// <summary>
    /// A feed that replays AlphaVantage historical data into DotQuant's event stream.
    /// </summary>
    public class AlphaVantageFeed : LiveFeed
    {
        private static readonly Currency USD = Currency.GetInstance("USD");
        public static readonly string Source = "alphavantage";

        private readonly IDataReader _dataReader;
        private readonly ILogger<AlphaVantageFeed> _logger;
        private readonly string[] _tickers;
        private readonly DateTime _start;
        private readonly DateTime _end;
        private readonly TimeSpan _timeSpan;

        public AlphaVantageFeed(
            IDataReader dataReader,
            ILogger<AlphaVantageFeed> logger,
            string[] tickers,
            DateTime start,
            DateTime end,
            TimeSpan? timeSpan = null) // Allow override if needed
        {
            _dataReader = dataReader;
            _logger = logger;
            _tickers = tickers;
            _start = start;
            _end = end;
            _timeSpan = timeSpan ?? TimeSpan.FromDays(1); // Default to daily bars
        }

        public override async Task Play(ChannelWriter<Event> channel, CancellationToken ct = default)
        {
            try
            {
                foreach (var ticker in _tickers)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!_dataReader.TryGetPrices(ticker, _start, _end, out var prices))
                    {
                        _logger.LogWarning("No price data for {Ticker} from {Start} to {End}", ticker, _start, _end);
                        continue;
                    }

                    var asset = new Stock(ticker, USD);

                    foreach (var price in prices)
                    {
                        ct.ThrowIfCancellationRequested();

                        var priceItem = new PriceItem(
                            asset,
                            open: price.Open,
                            high: price.High,
                            low: price.Low,
                            close: price.Close,
                            volume: price.Volume,
                            timeSpan: _timeSpan
                        );

                        var evt = new Event(price.Date, new List<PriceItem> { priceItem });

                        await channel.WriteAsync(evt, ct);
                        _logger.LogDebug("Emitted event for {Ticker} at {Time}", ticker, price.Date);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AlphaVantageFeed playback cancelled.");
            }
            finally
            {
                channel.TryComplete();
            }
        }
    }
}
