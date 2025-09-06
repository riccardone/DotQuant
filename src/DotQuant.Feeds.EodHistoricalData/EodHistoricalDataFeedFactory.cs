using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodHistoricalDataFeedFactory : LiveFeedFactoryBase
{
    public override string Key => "eod";
    public override string Name => "EOD Historical Data Feed";

    public override IFeed Create(
        IServiceProvider sp,
        IConfiguration config,
        ILogger logger,
        IDictionary<string, string?> args)
    {
        var apiKey = config["EOD:ApiKey"]
                     ?? throw new InvalidOperationException("Missing EOD API key in config under 'EOD:ApiKey'");

        // Parse standard feed arguments
        var (symbols, start, end, isLive) = ParseCommonArgs(args);
        var tickers = symbols.Select(s => s.ToString()).ToArray();

        // Optional polling interval
        TimeSpan? interval = null;
        if (args.TryGetValue("--interval", out var intervalArg) &&
            TimeSpan.TryParse(intervalArg, out var parsedInterval))
        {
            interval = parsedInterval;
        }

        // Optional fallback feed
        var fallbackFeeds = new List<IFeed>();
        if (args.TryGetValue("--fallback", out var fallbackKey) &&
            !string.IsNullOrWhiteSpace(fallbackKey))
        {
            var factories = sp.GetServices<IFeedFactory>();
            var fallbackFactory = factories.FirstOrDefault(f =>
                f.Key.Equals(fallbackKey, StringComparison.OrdinalIgnoreCase));

            if (fallbackFactory is null)
                throw new InvalidOperationException($"Unknown fallback feed: {fallbackKey}");

            var fallback = fallbackFactory.Create(sp, config, logger, args);
            fallbackFeeds.Add(fallback);
        }

        return new EodHistoricalDataFeed(
            logger: logger,
            httpFactory: sp.GetRequiredService<IHttpClientFactory>(),
            config: config,
            apiKey: apiKey,
            symbols: tickers,
            interval: interval,
            fallbackFeeds: fallbackFeeds,
            start: start,
            end: end,
            isLive: isLive
        );
    }
}
