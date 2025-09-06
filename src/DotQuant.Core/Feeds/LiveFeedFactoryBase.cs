using DotQuant.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Feeds;

public abstract class LiveFeedFactoryBase : IFeedFactory
{
    public abstract string Key { get; }
    public abstract string Name { get; }

    protected (Symbol[] symbols, DateTime start, DateTime end, bool isLive) ParseCommonArgs(
        IDictionary<string, string?> args)
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

        return (symbols, start, end, isLive);
    }

    public abstract IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args);
}