using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotQuant.Brokers.IBKR;

public sealed class InteractiveBrokersFeedFactory : IFeedFactory
{
    public string Key => "ibkr";
    public string Name => "Interactive Brokers Live Feed";

    public IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        // Bind options from "IBKR" section if present
        var section = config.GetSection("IBKR");
        var ibkrConfig = new IBKRConfig();
        section.Bind(ibkrConfig);

        // Optional ticker list from CLI: --tickers "AAPL,MSFT"
        var tickers = (args.TryGetValue("--tickers", out var list) ? list : "AAPL")
                      ?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                      ?? Array.Empty<string>();

        // Use DI to create IOptions<IBKRConfig>
        var options = Options.Create(ibkrConfig);
        var feed = new InteractiveBrokersLiveFeed(options);

        // Subscribe each ticker (fire-and-forget)
        foreach (var t in tickers)
        {
            _ = feed.ResolveCurrencyAndSubscribe(t);
        }

        return feed;
    }
}