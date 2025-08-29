using DotQuant.Core.Feeds;
using DotQuant.Feeds.EodHistoricalData;
using DotQuant.Feeds.YahooFinance;
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
        var yahooFactory = sp.GetRequiredService<YahooFinanceFeedFactory>();
        var eodFactory = sp.GetRequiredService<EodHistoricalDataFeedFactory>();
        var fallbackLogger = sp.GetRequiredService<ILogger<FallbackFeed>>();

        var settings = args is Dictionary<string, string?> dict
            ? dict
            : args.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var yahooFeed = yahooFactory.Create(sp, config, logger, settings);
        var eodFeed = eodFactory.Create(sp, config, logger, settings);

        return new FallbackFeed((YahooFinanceFeed)yahooFeed, (EodHistoricalDataFeed)eodFeed, fallbackLogger);
    }
}