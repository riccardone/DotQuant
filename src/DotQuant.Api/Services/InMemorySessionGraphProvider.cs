using DotQuant.Api.Models;

namespace DotQuant.Api.Services;

public class InMemorySessionGraphProvider : ISessionGraphProvider
{
    public Task<SessionGraphData> GetGraphDataAsync()
    {
        var prices = new List<PricePoint>
        {
            new(DateTime.UtcNow.AddMinutes(-3), 100, 102, 99, 101),
            new(DateTime.UtcNow.AddMinutes(-2), 101, 103, 100, 102),
            new(DateTime.UtcNow.AddMinutes(-1), 102, 104, 101, 103)
        };

        var signals = new List<SignalPoint>
        {
            new(DateTime.UtcNow.AddMinutes(-2), "Buy", 88),
            new(DateTime.UtcNow.AddMinutes(-1), "Sell", 92)
        };

        var orders = new List<OrderPoint>
        {
            new(DateTime.UtcNow.AddMinutes(-1), "Buy", 102.3m, 100),
            new(DateTime.UtcNow, "Sell", 103.5m, 100)
        };

        return Task.FromResult(new SessionGraphData(prices, signals, orders));
    }
}