using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.LiveFeedWithFallBack;

public class FallbackFeedFactory : IFeedFactory
{
    public string Key => "fallback";
    public string Name => "Fallback Feed";

    public IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        var symbolCsv = args.GetValueOrDefault("--tickers") ?? throw new ArgumentException("Missing --tickers");
        var intervalStr = args.GetValueOrDefault("--interval") ?? "00:01:00";
        var interval = TimeSpan.Parse(intervalStr);

        var symbols = symbolCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var fallbackLogger = loggerFactory.CreateLogger<FallbackFeed>();
        var feedMap = new Dictionary<string, (IEodSymbolFeed Eod, IYahooSymbolFeed Yahoo)>();

        foreach (var symbol in symbols)
        {
            var eod = new EodHistoricalDataFeedSingle(symbol, config, sp.GetRequiredService<IHttpClientFactory>(), fallbackLogger, interval);
            var yahoo = new YahooFinanceFeedSingle(symbol, config, sp.GetRequiredService<IHttpClientFactory>(), fallbackLogger, interval);
            feedMap[symbol] = (eod, yahoo);
        }

        return new FallbackFeed(feedMap, fallbackLogger, interval);
    }
}