using DotQuant.Core.Services.GraphModels;

namespace DotQuant.Core.Services;

public interface ISessionGraphProvider
{
    Task<SessionGraphData> GetGraphDataAsync();

    void AddPrice(PricePoint point);
    void AddSignal(SignalPoint point);
    void AddOrder(OrderPoint point);
}