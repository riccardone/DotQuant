using DotQuant.Core.Brokers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotQuant.Brokers.IBKR;

public class IBKRBrokerFactory : IBrokerFactory
{
    public string Key => "ibkr";
    public string DisplayName => "Interactive Brokers Broker";
    public string Description => "Connects to Interactive Brokers via TWS or IB Gateway.";

    public IBroker Create(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var section = config.GetSection("IBKR");
        var ibkrConfig = new IBKRConfig();
        section.Bind(ibkrConfig);

        var brokerLogger = services.GetRequiredService<ILogger<IBKRBroker>>();
        return new IBKRBroker(brokerLogger, ibkrConfig);
    }
}
