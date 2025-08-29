using DotQuant.Core.Feeds;
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
        var apiKey = config["EOD:ApiKey"] ?? throw new InvalidOperationException("Missing EOD API key in config under 'EOD:ApiKey'");

        if (!args.TryGetValue("--tickers", out var tickersArg) || string.IsNullOrWhiteSpace(tickersArg))
            throw new ArgumentException("Missing --tickers argument");

        var tickers = tickersArg.Split(',');
        var intervalSeconds = int.TryParse(config["EOD:PollingIntervalSeconds"], out var s) ? s : 30;

        return new EodHistoricalDataFeed(
            logger,
            sp.GetRequiredService<IHttpClientFactory>(),
            config,
            apiKey,
            tickers,
            TimeSpan.FromSeconds(intervalSeconds));
    }
}