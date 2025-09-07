namespace DotQuant.Core.Services.GraphModels;

public record SessionGraphData(
    List<PricePoint> Prices,
    List<SignalPoint> Signals,
    List<OrderPoint> Orders
);