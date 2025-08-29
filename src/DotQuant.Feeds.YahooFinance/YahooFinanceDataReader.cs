using System.Text.Json;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Feeds.Model;

namespace DotQuant.Feeds.YahooFinance;

/// <summary>
/// Reads historical and latest price data from Yahoo's v8/chart API.
/// </summary>
public class YahooFinanceDataReader() : IDataReader
{
    private static readonly HttpClient _httpClient = new();
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

            // Use "range=" for recent timeframes (less than 30 days)
            var daysRange = (endDate.Date - startDate.Date).TotalDays;

            if (daysRange <= 30 && endDate.Date >= DateTime.UtcNow.Date.AddDays(-30))
            {
                // Choose appropriate range string for Yahoo
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
                // Use explicit timestamps for older/historical ranges
                long period1 = new DateTimeOffset(startDate.Date).ToUnixTimeSeconds();
                long period2 = new DateTimeOffset(endDate.Date.AddDays(1)).ToUnixTimeSeconds(); // ensure period2 > period1
                url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&period1={period1}&period2={period2}";
            }

            var json = _httpClient.GetStringAsync(url).Result;
            var parsedPrices = ParseChartPrices(json);

            if (parsedPrices == null)
                return false;

            // Filter to the exact range the caller asked for
            prices = parsedPrices.Where(p => p.Date.Date >= startDate.Date && p.Date.Date <= endDate.Date).ToList();
            return prices.Any();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Yahoo] Failed to fetch prices for {symbol}: {ex.Message}");
            return false;
        }
    }

    public bool TryGetLatestPrice(Symbol symbol, out Price? price)
    {
        price = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                string ticker = symbol.ToString();
                string url = BuildChartUrlWithRange(ticker, "5d", "1d");

                var json = _httpClient.GetStringAsync(url).Result;
                var prices = ParseChartPrices(json);

                price = prices?.LastOrDefault();
                return price != null;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                Console.WriteLine($"[Yahoo] Retry {attempt} failed for {symbol}: {ex.Message}");
                Thread.Sleep(_throttleMs);
            }
        }

        return false;
    }

    private static string BuildChartUrlWithPeriods(string ticker, DateTime from, DateTime to, string interval)
    {
        long period1 = new DateTimeOffset(from).ToUnixTimeSeconds();
        long period2 = new DateTimeOffset(to).ToUnixTimeSeconds();
        return $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval={interval}&period1={period1}&period2={period2}";
    }

    private static string BuildChartUrlWithRange(string ticker, string range, string interval)
    {
        return $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval={interval}&range={range}";
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
            Console.WriteLine($"[Yahoo] Failed to parse response: {ex.Message}");
            return null;
        }
    }
}
