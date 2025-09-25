using DotQuant.Core.MarketData;

namespace DotQuant.Brokers.IBKR;

public interface IInteractiveBrokersClient
{
    Task ConnectAsync(string host, int port, int clientId, CancellationToken ct);
    Task SubscribeAsync(string symbol, string exchange, string currency, CancellationToken ct);
    IAsyncEnumerable<Tick> GetTicksAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
}