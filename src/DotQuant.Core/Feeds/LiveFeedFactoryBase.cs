using DotQuant.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Feeds;

public record FeedArgs(Symbol[] Symbols, DateTime Start, DateTime End, bool IsLive, TimeSpan? Interval);

public abstract class LiveFeedFactoryBase : IFeedFactory
{
    public abstract string Key { get; }
    public abstract string Name { get; }

    protected FeedArgs ParseCommonArgs(IDictionary<string, string?> args)
    {
        if (!args.TryGetValue("--tickers", out var tickersStr) || string.IsNullOrWhiteSpace(tickersStr))
            throw new ArgumentException("Missing '--tickers' argument");

        var symbols = tickersStr
            .Split(',')
            .Select(s =>
            {
                var parts = s.Split('.', 2);
                return new Symbol(parts[0], parts.Length > 1 ? parts[1] : "UNKNOWN");
            })
            .ToArray();

        var isLive = args.TryGetValue("--live", out var liveStr) && bool.TryParse(liveStr, out var live) && live;

        if (isLive && (args.ContainsKey("--start") || args.ContainsKey("--end")))
            throw new ArgumentException("'--live' mode cannot be used with '--start' or '--end'.");

        DateTime start, end;
        if (isLive)
        {
            start = DateTime.UtcNow;
            end = DateTime.UtcNow;
        }
        else
        {
            start = args.TryGetValue("--start", out var startStr) && DateTime.TryParse(startStr, out var s)
                ? s
                : throw new ArgumentException("Missing or invalid '--start'");

            end = args.TryGetValue("--end", out var endStr) && DateTime.TryParse(endStr, out var e)
                ? e
                : throw new ArgumentException("Missing or invalid '--end'");
        }

        TimeSpan? interval = null;
        if (args.TryGetValue("--interval", out var intervalStr) && !string.IsNullOrWhiteSpace(intervalStr))
        {
            if (TimeSpan.TryParse(intervalStr, out var parsed))
                interval = parsed;
        }

        return new FeedArgs(symbols, start, end, isLive, interval);
    }

    public abstract IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args);
}
