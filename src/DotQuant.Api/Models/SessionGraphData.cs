namespace DotQuant.Api.Models;

public record SessionGraphData(
    List<PricePoint> Prices,
    List<SignalPoint> Signals,
    List<OrderPoint> Orders
);