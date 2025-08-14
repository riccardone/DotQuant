namespace DotQuant.Core.Common;

public record Position(Size Size, decimal AveragePrice, decimal MarketPrice)
{
    // TODO

    public bool Closed { get; set; } 
    public bool Open { get; set; }
    public bool Long { get; set; }

    public static Position? Empty()
    {
        // TODO ??
        return new Position(new Size(0.0m), 0.0m, 0.0m);
    }
}