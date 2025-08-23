using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Brokers;

public interface IBrokerFactory
{
    string Key { get; }
    string DisplayName { get; }
    string Description { get; }

    IBroker Create(IServiceProvider services, IConfiguration config, ILogger logger);
}