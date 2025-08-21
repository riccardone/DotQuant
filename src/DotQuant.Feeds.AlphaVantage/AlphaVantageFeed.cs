using System.Threading.Channels;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.MarketData;
using DotQuant.Feeds.AlphaVantage.AlphaVantage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    public class AlphaVantageFeedFactory : IFeedFactory
    {
        public string Key => "av";
        public string Name => "AlphaVantage Feed";

        public IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
        {
            var priceVolumeProvider = sp.GetRequiredService<IPriceVolumeProvider>();
            var dataFetcher = sp.GetRequiredService<DataFetcher>();
            var loggerInstanceForReader = sp.GetRequiredService<ILogger<AlphaVantageDataReader>>();
            var loggerInstanceForFeed = sp.GetRequiredService<ILogger<AlphaVantageFeed>>();

            var dataReader = new AlphaVantageDataReader(priceVolumeProvider, loggerInstanceForReader, dataFetcher);

            return new AlphaVantageFeed(dataReader, loggerInstanceForFeed);
        }
    }
}
