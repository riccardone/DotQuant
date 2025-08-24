using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Services;
using DotQuant.Feeds.AlphaVantage.AlphaVantage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.AlphaVantage;

public class AlphaVantageFeedFactory : IFeedFactory
{
    public string Key => "av";
    public string Name => "AlphaVantage Feed";

    public IFeed Create(IServiceProvider sp, IConfiguration config, ILogger logger, IDictionary<string, string?> args)
    {
        var apiKey = config["AlphaVantage:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("AlphaVantage API key is required. Set AlphaVantage:ApiKey in appsettings.");

        if (!args.TryGetValue("--tickers", out var rawTickers) || string.IsNullOrWhiteSpace(rawTickers))
            throw new ArgumentException("Missing required --tickers argument (comma-separated list)");

        var symbols = rawTickers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseSymbol)
            .ToArray();

        if (symbols.Length == 0)
            throw new ArgumentException("No valid tickers provided in --tickers argument");

        var isLive = args.TryGetValue("--live", out var liveArg) &&
                     (liveArg?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);

        var start = isLive ? DateTime.UtcNow : ParseDate(args, config, "Start", DateTime.UtcNow.AddYears(-1));
        var end = isLive ? DateTime.UtcNow : ParseDate(args, config, "End", DateTime.UtcNow);
        var pollingInterval = ParseTimeSpan(args, config, "PollingInterval", TimeSpan.FromMinutes(1));

        logger.LogInformation("AlphaVantageFeed mode: {Mode}", isLive ? "LIVE" : "HISTORICAL");

        var priceVolumeProvider = sp.GetRequiredService<IPriceVolumeProvider>();
        var dataFetcher = sp.GetRequiredService<DataFetcher>();
        var loggerForReader = sp.GetRequiredService<ILogger<AlphaVantageDataReader>>();
        var loggerForFeed = sp.GetRequiredService<ILogger<AlphaVantageFeed>>();
        var marketStatus = sp.GetRequiredService<IMarketStatusService>();

        var dataReader = new AlphaVantageDataReader(priceVolumeProvider, loggerForReader, dataFetcher);

        return new AlphaVantageFeed(
            dataReader,
            loggerForFeed,
            symbols,
            start,
            end,
            isLiveMode: isLive,
            pollingInterval: pollingInterval,
            marketStatusService: marketStatus
        );
    }

    private static Symbol ParseSymbol(string input)
    {
        var parts = input.Split('.', 2);
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid symbol format: '{input}'. Expected format TICKER.EXCHANGE");

        return new Symbol(parts[0], parts[1]);
    }

    private static DateTime ParseDate(IDictionary<string, string?> args, IConfiguration config, string key, DateTime fallback)
    {
        if (args.TryGetValue($"--{key.ToLower()}", out var argValue) && DateTime.TryParse(argValue, out var fromArgs))
            return fromArgs;

        var configValue = config[$"AlphaVantage:{key}"];
        if (!string.IsNullOrWhiteSpace(configValue) && DateTime.TryParse(configValue, out var fromConfig))
            return fromConfig;

        return fallback;
    }

    private static TimeSpan ParseTimeSpan(IDictionary<string, string?> args, IConfiguration config, string key, TimeSpan fallback)
    {
        if (args.TryGetValue($"--{key.ToLower()}", out var argValue) && TimeSpan.TryParse(argValue, out var parsedSpan))
            return parsedSpan;

        var configValue = config[$"AlphaVantage:{key}"];
        if (!string.IsNullOrWhiteSpace(configValue) && TimeSpan.TryParse(configValue, out var parsedFromConfig))
            return parsedFromConfig;

        return fallback;
    }
}
