using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.YahooFinance;

public class YahooFinanceFeedFactory : LiveFeedFactoryBase
{
    public override string Key => "yahoo";
    public override string Name => "Yahoo Finance Feed";

    public override IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        var (symbols, start, end, isLive) = ParseCommonArgs(args);

        return new YahooFinanceFeed(
            dataReader: new YahooFinanceDataReader(sp.GetRequiredService<ILogger<YahooFinanceDataReader>>()),
            logger: sp.GetRequiredService<ILogger<YahooFinanceFeed>>(),
            symbols: symbols,
            start: start,
            end: end,
            marketStatusService: sp.GetRequiredService<IMarketStatusService>(),
            isLiveMode: isLive,
            pollingInterval: TimeSpan.FromSeconds(10),
            config: config 
        );
    }
}