using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.YahooFinance;

public class YahooFinanceFeedFactory : IFeedFactory
{
    public string Key => "yahoo";
    public string Name => "Yahoo Finance Feed";

    public IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        var symbols = args.ContainsKey("symbols")
            ? args["symbols"]
                ?.Split(',')
                .Select(s =>
                {
                    var parts = s.Split('.', 2);
                    return new Symbol(parts[0], parts[1]);
                })
                .ToArray() ?? Array.Empty<Symbol>()
            : throw new ArgumentException("Missing 'symbols' argument for YahooFinanceFeed");

        var start = args.TryGetValue("start", out var startStr) && DateTime.TryParse(startStr, out var s) ? s : DateTime.UtcNow.AddDays(-5);
        var end = args.TryGetValue("end", out var endStr) && DateTime.TryParse(endStr, out var e) ? e : DateTime.UtcNow;

        var live = args.TryGetValue("live", out var liveStr) && bool.TryParse(liveStr, out var isLive) && isLive;

        var dataReader = new YahooFinanceDataReader();
        var feedLogger = sp.GetRequiredService<ILogger<YahooFinanceFeed>>();
        var marketStatus = sp.GetRequiredService<IMarketStatusService>();

        return new YahooFinanceFeed(
            dataReader,
            feedLogger,
            symbols,
            start,
            end,
            marketStatus,
            live
        );
    }
}