using DotQuant.Core.Feeds;
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
        // Get and validate API key
        var apiKey = config["AlphaVantage:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("AlphaVantage API key is required. Set AlphaVantage:ApiKey in appsettings.");
        }

        // Parse tickers (required)
        var tickersArg = args.TryGetValue("--tickers", out var rawTickers) ? rawTickers : null;
        if (string.IsNullOrWhiteSpace(tickersArg))
        {
            throw new ArgumentException("Missing required --tickers argument (comma-separated list)");
        }

        var tickers = tickersArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tickers.Length == 0)
        {
            throw new ArgumentException("No valid tickers provided in --tickers argument");
        }

        // Start/end date from config or fallback
        var start = ParseDate(args, config, "Start", fallback: DateTime.UtcNow.AddYears(-1));
        var end = ParseDate(args, config, "End", fallback: DateTime.UtcNow);

        // Optional: support custom TimeSpan
        var timeSpan = TimeSpan.FromDays(1); // Could make this dynamic if needed later

        // Resolve services
        var priceVolumeProvider = sp.GetRequiredService<IPriceVolumeProvider>();
        var dataFetcher = sp.GetRequiredService<DataFetcher>();
        var loggerForReader = sp.GetRequiredService<ILogger<AlphaVantageDataReader>>();
        var loggerForFeed = sp.GetRequiredService<ILogger<AlphaVantageFeed>>();

        var dataReader = new AlphaVantageDataReader(priceVolumeProvider, loggerForReader, dataFetcher);

        return new AlphaVantageFeed(dataReader, loggerForFeed, tickers, start, end, timeSpan);
    }

    private static DateTime ParseDate(IDictionary<string, string?> args, IConfiguration config, string key, DateTime fallback)
    {
        // Try CLI arg first
        if (args.TryGetValue($"--{key.ToLower()}", out var fromArgs) && DateTime.TryParse(fromArgs, out var parsedFromArgs))
            return parsedFromArgs;

        // Then config
        var fromConfig = config[$"AlphaVantage:{key}"];
        if (!string.IsNullOrWhiteSpace(fromConfig) && DateTime.TryParse(fromConfig, out var parsedFromConfig))
            return parsedFromConfig;

        // Fallback
        return fallback;
    }
}
