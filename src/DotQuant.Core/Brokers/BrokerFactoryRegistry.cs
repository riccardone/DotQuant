namespace DotQuant.Core.Brokers;

public class BrokerFactoryRegistry : IBrokerFactoryRegistry
{
    private readonly Dictionary<string, IBrokerFactory> _factories;

    public BrokerFactoryRegistry(IEnumerable<IBrokerFactory> factories)
    {
        _factories = factories.ToDictionary(f => f.Key.ToLowerInvariant());
    }

    public IEnumerable<string> Keys => _factories.Keys;

    public IEnumerable<IBrokerFactory> All => _factories.Values;

    public IBrokerFactory? Get(string key)
    {
        _factories.TryGetValue(key.ToLowerInvariant(), out var factory);
        return factory;
    }
}