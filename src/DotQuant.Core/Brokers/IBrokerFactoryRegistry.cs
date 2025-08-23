namespace DotQuant.Core.Brokers;

public interface IBrokerFactoryRegistry
{
    IEnumerable<string> Keys { get; }
    IBrokerFactory? Get(string key);
    IEnumerable<IBrokerFactory> All { get; }
}