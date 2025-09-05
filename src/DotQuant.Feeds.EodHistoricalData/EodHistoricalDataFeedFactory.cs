
﻿using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodHistoricalDataFeedFactory : IFeedFactory
{
    public string Key => "eod";
    public string Name => "EOD Historical Data Feed";

    public IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        var apiKey = config["EOD:ApiKey"]
                     ?? throw new InvalidOperationException("Missing EOD API key in config under 'EOD:ApiKey'");

        if (!args.TryGetValue("--tickers", out var tickersArg) || string.IsNullOrWhiteSpace(tickersArg))
            throw new ArgumentException("Missing --tickers argument");

        var tickers = tickersArg.Split(',');

        TimeSpan interval = TimeSpan.FromSeconds(30);
        if (args.TryGetValue("--interval", out var intervalArg) && TimeSpan.TryParse(intervalArg, out var parsedInterval))
        {
            interval = parsedInterval;
        }

        // 🔁 Check for fallback feed key
        var fallbackFeeds = new List<IFeed>();
        if (args.TryGetValue("--fallback", out var fallbackKey) && !string.IsNullOrWhiteSpace(fallbackKey))
        {
            var factories = sp.GetServices<IFeedFactory>();
            var fallbackFactory = factories.FirstOrDefault(f => f.Key.Equals(fallbackKey, StringComparison.OrdinalIgnoreCase));

            if (fallbackFactory is null)
                throw new InvalidOperationException($"Unknown fallback feed: {fallbackKey}");

            var fallback = fallbackFactory.Create(sp, config, logger, args);
            fallbackFeeds.Add(fallback);
        }

        return new EodHistoricalDataFeed(
            logger,
            sp.GetRequiredService<IHttpClientFactory>(),
            config,
            apiKey,
            tickers,
            interval,
            fallbackFeeds);
    }
}
