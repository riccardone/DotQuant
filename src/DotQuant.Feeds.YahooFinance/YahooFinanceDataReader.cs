using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Feeds.Model;
using Microsoft.Extensions.Options;
using YahooFinanceApi;

namespace DotQuant.Feeds.YahooFinance;

/// <summary>
/// Reads historical and latest price data from Yahoo Finance.
/// </summary>
public class YahooFinanceDataReader() : IDataReader
{
    private readonly YahooFinanceOptions _options = new()
    {
        MaxRetries = 3,
        ThrottleMs = 500
    };

    public bool TryGetPrices(Symbol symbol, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices)
    {
        prices = null;
        try
        {
            var candles = Yahoo
                .GetHistoricalAsync(symbol.Ticker, startDate, endDate, Period.Daily)
                .GetAwaiter()
                .GetResult();

            prices = candles.Select(c => new Price
            {
                Date = c.DateTime,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = (long)c.Volume
            });

            return prices.Any();
        }
        catch
        {
            return false;
        }
    }

    public bool TryGetLatestPrice(Symbol symbol, out Price? price)
    {
        price = null;
        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                var quotes = Yahoo
                    .GetHistoricalAsync(symbol.Ticker, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, Period.Daily)
                    .GetAwaiter()
                    .GetResult();

                var quote = quotes.LastOrDefault();
                if (quote == null) return false;

                price = new Price
                {
                    Date = quote.DateTime,
                    Open = quote.Open,
                    High = quote.High,
                    Low = quote.Low,
                    Close = quote.Close,
                    Volume = (long)quote.Volume
                };

                return true;
            }
            catch when (attempt < _options.MaxRetries)
            {
                Thread.Sleep(_options.ThrottleMs);
            }
        }

        return false;
    }
}
