namespace DotQuant.Core.Services.GraphModels;

public record SignalPoint(string Ticker, DateTime Time, string Type, int Confidence);