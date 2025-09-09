using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodWebSocketFeedFactory : LiveFeedFactoryBase
{
    public override string Key => "eodws";
    public override string Name => "EOD WebSocket Feed";

    public override IFeed Create(
        IServiceProvider sp,
        IConfiguration config,
        ILogger logger,
        IDictionary<string, string?> args)
    {
        var apiKey = config["EOD:ApiKey"]
                     ?? throw new InvalidOperationException("Missing EOD API key in config under 'EOD:ApiKey'");

        var (symbols, _, _, _) = ParseCommonArgs(args);
        var tickers = symbols.Select(s => s.ToString()).ToArray();

        return new EodWebSocketFeed(
            apiKey: apiKey,
            symbols: tickers,
            logger: sp.GetRequiredService<ILogger<EodWebSocketFeed>>(),
            config: config,
            marketStatusService: sp.GetRequiredService<IMarketStatusService>()
        );
    }
}