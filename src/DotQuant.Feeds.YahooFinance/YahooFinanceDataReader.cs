using System.Text.Json;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Feeds.Model;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.YahooFinance;

public class YahooFinanceDataReader(ILogger<YahooFinanceDataReader> logger) : IDataReader
{
    private static readonly HttpClient _httpClient = new();
    private readonly ILogger<YahooFinanceDataReader> _logger = logger;
    private readonly int _maxRetries = 3;
    private readonly int _throttleMs = 500;

    static YahooFinanceDataReader()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public bool TryGetPrices(Symbol symbol, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices)
    {
        prices = null;

        try
        {
            string ticker = symbol.ToString();
            string url;

            var daysRange = (endDate.Date - startDate.Date).TotalDays;

            if (daysRange <= 30 && endDate.Date >= DateTime.UtcNow.Date.AddDays(-30))
            {
                string range = daysRange switch
                {
                    <= 5 => "5d",
                    <= 10 => "10d",
                    <= 30 => "1mo",
                    _ => "1mo"
                };

                url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range={range}";
            }
            else
            {
                long period1 = new DateTimeOffset(startDate.Date).ToUnixTimeSeconds();
                long period2 = new DateTimeOffset(endDate.Date.AddDays(1)).ToUnixTimeSeconds();
                url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&period1={period1}&period2={period2}";
            }

            _logger.LogDebug("[Yahoo] Fetching historical prices from URL: {Url}", url);
            var json = _httpClient.GetStringAsync(url).Result;

            var parsedPrices = ParseChartPrices(json);
            if (parsedPrices == null)
            {
                _logger.LogWarning("[Yahoo] Could not parse historical data for {Symbol}", symbol);
                return false;
            }

            prices = parsedPrices
                .Where(p => p.Date.Date >= startDate.Date && p.Date.Date <= endDate.Date)
                .ToList();

            _logger.LogInformation("[Yahoo] Retrieved {Count} prices for {Symbol}", prices.Count(), symbol);
            return prices.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Yahoo] Failed to fetch historical prices for {Symbol}", symbol);
            return false;
        }
    }

    public bool TryGetLatestPrice(Symbol symbol, out Price? price)
    {
        price = null;
        string ticker = symbol.ToString();

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var url = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={ticker}";
                _logger.LogDebug("[Yahoo] Fetching real-time quote from URL: {Url}", url);

                var json = _httpClient.GetStringAsync(url).Result;

                using var doc = JsonDocument.Parse(json);
                var quote = doc.RootElement
                    .GetProperty("quoteResponse")
                    .GetProperty("result")[0];

                var priceObj = new Price
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(quote.GetProperty("regularMarketTime").GetInt64()).UtcDateTime,
                    Open = quote.GetProperty("regularMarketOpen").GetDecimal(),
                    High = quote.GetProperty("regularMarketDayHigh").GetDecimal(),
                    Low = quote.GetProperty("regularMarketDayLow").GetDecimal(),
                    Close = quote.GetProperty("regularMarketPrice").GetDecimal(),
                    Volume = quote.TryGetProperty("regularMarketVolume", out var volumeProp) && volumeProp.ValueKind != JsonValueKind.Null
                        ? volumeProp.GetInt64()
                        : 0
                };

                price = priceObj;

                _logger.LogInformation("[Yahoo] Real-time price for {Symbol}: {Price} at {Time}",
                    symbol, price.Close, price.Date);

                return true;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                _logger.LogWarning(ex, "[Yahoo] Retry {Attempt}/{Max} failed for {Symbol}", attempt, _maxRetries, symbol);
                Thread.Sleep(_throttleMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Yahoo] Failed to fetch real-time quote for {Symbol}", symbol);
                return false;
            }
        }

        return false;
    }

    private static IEnumerable<Price>? ParseChartPrices(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var timestamps = root.GetProperty("timestamp").EnumerateArray().ToArray();
            var indicators = root.GetProperty("indicators").GetProperty("quote")[0];

            var opens = indicators.GetProperty("open").EnumerateArray().ToArray();
            var highs = indicators.GetProperty("high").EnumerateArray().ToArray();
            var lows = indicators.GetProperty("low").EnumerateArray().ToArray();
            var closes = indicators.GetProperty("close").EnumerateArray().ToArray();
            var volumes = indicators.GetProperty("volume").EnumerateArray().ToArray();

            var prices = new List<Price>();

            for (int i = 0; i < timestamps.Length; i++)
            {
                if (opens[i].ValueKind == JsonValueKind.Null || closes[i].ValueKind == JsonValueKind.Null)
                    continue;

                prices.Add(new Price
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime,
                    Open = opens[i].GetDecimal(),
                    High = highs[i].GetDecimal(),
                    Low = lows[i].GetDecimal(),
                    Close = closes[i].GetDecimal(),
                    Volume = volumes[i].GetInt64()
                });
            }

            return prices;
        }
        catch (Exception ex)
        {
            // No logger here – parse fallback
            return null;
        }
    }
}
