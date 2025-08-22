using System.Threading.Channels;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.MarketData;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.AlphaVantage
{
    public class AlphaVantageFeed : IFeed
    {
        private readonly IDataReader _dataReader;
        private readonly ILogger<AlphaVantageFeed> _logger;

        public string Source => "alphavantage";

        public AlphaVantageFeed(IDataReader dataReader, ILogger<AlphaVantageFeed> logger)
        {
            _dataReader = dataReader;
            _logger = logger;
        }

        public IEnumerable<Tick> GetHistoricalTicks(string ticker, DateTime start, DateTime end)
        {
            if (!_dataReader.TryGetPrices(ticker, start, end, out var prices))
            {
                _logger.LogWarning("No price data available for {Ticker} from {Start} to {End}", ticker, start, end);
                return Enumerable.Empty<Tick>();
            }

            return prices.Select(p => new Tick(
                Symbol: ticker,
                Exchange: "N/A",  // Modify if exchange data becomes available
                Currency: "USD",  // Adjust if dynamic currency detection is added
                Timestamp: p.Date,
                BidPrice: null,
                AskPrice: null,
                LastPrice: p.Close,
                BidSize: null,
                AskSize: null,
                LastSize: p.Volume
            ));
        }

        public Task Play(ChannelWriter<Event> channel, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
