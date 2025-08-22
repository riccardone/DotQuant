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
        var apiKey = config["AlphaVantage:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("AlphaVantage API key is required for this feed. " +
                                                "Please set AlphaVantage:ApiKey in appsettings or environment variables.");
        }
     
        var priceVolumeProvider = sp.GetRequiredService<IPriceVolumeProvider>();
        var dataFetcher = sp.GetRequiredService<DataFetcher>();
        var loggerForReader = sp.GetRequiredService<ILogger<AlphaVantageDataReader>>();
        var loggerForFeed = sp.GetRequiredService<ILogger<AlphaVantageFeed>>();

        var dataReader = new AlphaVantageDataReader(priceVolumeProvider, loggerForReader, dataFetcher);

        return new AlphaVantageFeed(dataReader, loggerForFeed);
    }
}