using DotQuant.Core.Services.GraphModels;

namespace DotQuant.Core.Services;

public class InMemorySessionGraphProvider : ISessionGraphProvider
{
    private readonly List<PricePoint> _prices = new();
    private readonly List<SignalPoint> _signals = new();
    private readonly List<OrderPoint> _orders = new();
    private readonly object _lock = new();

    public void AddPrice(PricePoint price)
    {
        lock (_lock)
        {
            _prices.Add(price);
        }
    }

    public void AddSignal(SignalPoint signal)
    {
        lock (_lock)
        {
            _signals.Add(signal);
        }
    }

    public void AddOrder(OrderPoint order)
    {
        lock (_lock)
        {
            _orders.Add(order);
        }
    }

    public Task<SessionGraphData> GetGraphDataAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(new SessionGraphData(
                Prices: _prices.ToList(),
                Signals: _signals.ToList(),
                Orders: _orders.ToList()
            ));
        }
    }
}