namespace DotQuant.Api.Models;

public record OrderPoint(DateTime Time, string Side, decimal Price, int Quantity);