using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Feeds.Model;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.AlphaVantage.AlphaVantage;

public class AlphaVantageDataReader : IDataReader
{
    private readonly ILogger<AlphaVantageDataReader> _logger;
    private readonly DataFetcher _dataFetcher;
    private readonly IPriceVolumeProvider _priceVolumeProvider;

    public AlphaVantageDataReader(
        IPriceVolumeProvider priceVolumeProvider,
        ILogger<AlphaVantageDataReader> logger,
        DataFetcher dataFetcher)
    {
        _dataFetcher = dataFetcher;
        _priceVolumeProvider = priceVolumeProvider;
        _logger = logger;
    }

    public bool TryGetPrices(Symbol symbol, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices)
    {
        const int maxCompactDays = 100;
        var useFull = (DateTime.UtcNow.Date - startDate.Date).TotalDays > maxCompactDays;
        var outputSize = useFull ? "full" : "compact";

        var key = $"daily_{symbol}";
        var query = $"query?function=TIME_SERIES_DAILY&symbol={symbol.Ticker}&outputsize={outputSize}";

        if (_dataFetcher.TryLoadOrFetch<TimeSeriesDailyResponse, List<Price>>(key, query, raw =>
        {
            if (raw.TimeSeries == null)
                return new List<Price>();

            var results = new List<Price>();

            foreach (var (dateStr, values) in raw.TimeSeries)
            {
                if (!DateTime.TryParse(dateStr, out var date))
                    continue;

                if (date < startDate || date > endDate)
                    continue;

                if (!TryParseOHLCV(values, out var price))
                {
                    _logger.LogWarning("Skipping malformed entry for {Symbol} on {Date}", symbol, date);
                    continue;
                }

                price.Date = date;

                if (price.Volume <= 0)
                {
                    var fallbackVol = _priceVolumeProvider.GetVolume(symbol, date);
                    if (fallbackVol.HasValue)
                    {
                        price.Volume = fallbackVol.Value;
                        _logger.LogDebug("Fallback volume for {Symbol} on {Date}: {Volume}", symbol, date.ToString("d"), price.Volume);
                    }
                }

                results.Add(price);
            }

            return results.OrderBy(p => p.Date).ToList();

        }, out var parsedPrices))
        {
            prices = parsedPrices ?? Enumerable.Empty<Price>();
            return prices.Any();
        }

        _logger.LogError("Failed to fetch or parse daily prices for {Symbol}", symbol);
        prices = null;
        return false;
    }

    public bool TryGetLatestPrice(Symbol symbol, out Price? price)
    {
        price = null;

        var key = $"intraday_1min_{symbol}";
        var query = $"query?function=TIME_SERIES_INTRADAY&symbol={symbol.Ticker}&interval=1min&outputsize=compact";

        if (_dataFetcher.TryLoadOrFetchRawJson(key, query, json =>
        {
            var parsed = IntradayTimeSeriesResponse.FromJson(json);
            if (parsed?.TimeSeries is null || parsed.TimeSeries.Count == 0)
                return null;

            var (timestamp, data) = parsed.TimeSeries.OrderByDescending(kvp => kvp.Key).First();

            if (!DateTime.TryParse(timestamp, out var date))
                return null;

            if (!TryParseOHLCV(data, out var parsedPrice))
                return null;

            parsedPrice.Date = date;
            return new List<Price> { parsedPrice };

        }, out var result))
        {
            price = result?.FirstOrDefault();
            return price != null;
        }

        return false;
    }

    private bool TryParseOHLCV(Dictionary<string, string> raw, out Price price)
    {
        price = default!;

        if (!decimal.TryParse(raw.GetValueOrDefault("1. open"), out var open)) return false;
        if (!decimal.TryParse(raw.GetValueOrDefault("2. high"), out var high)) return false;
        if (!decimal.TryParse(raw.GetValueOrDefault("3. low"), out var low)) return false;
        if (!decimal.TryParse(raw.GetValueOrDefault("4. close"), out var close)) return false;

        var volumeStr = raw.GetValueOrDefault("5. volume") ?? raw.GetValueOrDefault("6. volume") ?? "0";
        if (!decimal.TryParse(volumeStr, out var volume)) return false;

        price = new Price
        {
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume
        };

        return true;
    }
}
