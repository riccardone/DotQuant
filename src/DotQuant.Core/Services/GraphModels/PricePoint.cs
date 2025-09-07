namespace DotQuant.Core.Services.GraphModels;

public record PricePoint(string Ticker, DateTime Time, decimal Open, decimal High, decimal Low, decimal Close);