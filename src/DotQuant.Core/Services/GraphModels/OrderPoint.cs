namespace DotQuant.Core.Services.GraphModels;

public record OrderPoint(string Ticker, DateTime Time, string Side, decimal Price, decimal Quantity);