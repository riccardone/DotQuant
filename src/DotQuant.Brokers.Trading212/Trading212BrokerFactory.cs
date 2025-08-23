using DotQuant.Core.Brokers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Brokers.Trading212;

public class Trading212BrokerFactory : IBrokerFactory
{
    public string Key => "trading212";
    public string DisplayName => "Trading 212 Broker";
    public string Description => "Connects to live Trading 212 account via API key.";

    public IBroker Create(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var token = config["Trading212:AuthToken"];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Trading212 token missing.");

        var http = services.GetRequiredService<HttpClient>();
        var brokerLogger = services.GetRequiredService<ILogger<Trading212Broker>>();
        return new Trading212Broker(http, brokerLogger, token);
    }
}