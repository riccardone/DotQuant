using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.Ibkr;

public class IbkrFeedFactory : LiveFeedFactoryBase
{
    public override string Key => "ibkr";
    public override string Name => "Interactive Brokers Feed";

    public override IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        var feedArgs = ParseCommonArgs(args); // FeedArgs, not tuple
        var symbolStrings = feedArgs.Symbols.Select(s => s.ToString()).ToArray();
        return new IbkrFeed(
            symbols: symbolStrings,
            logger: sp.GetRequiredService<ILogger<IbkrFeed>>(),
            config: config,
            marketStatusService: sp.GetRequiredService<IMarketStatusService>(),
            interval: feedArgs.Interval
        );
    }
}
