using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Brokers;

public class SimBrokerFactory : IBrokerFactory
{
    public string Key => "sim";
    public string DisplayName => "Simulated Broker";
    public string Description => "Offline simulated broker for backtesting.";

    public IBroker Create(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var simLogger = services.GetRequiredService<ILogger<SimBroker>>();
        return new SimBroker(simLogger, 100_000m, "USD");
    }
}