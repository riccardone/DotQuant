namespace DotQuant.Core.Common;

public class PriceItem : IItem
{
    public IAsset Asset { get; }
    public decimal Open { get; }
    public decimal High { get; }
    public decimal Low { get; }
    public decimal Close { get; private set; }
    public decimal Volume { get; }
    public TimeSpan TimeSpan { get; }

    // Exposes currency if the Asset is a Stock (or similar type)
    public Currency? Currency => Asset is Stock stock ? stock.Currency : null;

    public PriceItem(IAsset asset, decimal open, decimal high, decimal low, decimal close, decimal volume, TimeSpan timeSpan)
    {
        Asset = asset;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        TimeSpan = timeSpan;
    }

    public void AdjustClose(decimal adjustedClose)
    {
        Close = adjustedClose;
    }

    public decimal GetPrice(string type = "DEFAULT")
    {
        return type.ToLowerInvariant() switch
        {
            "open" => (decimal)Open,
            "high" => (decimal)High,
            "low" => (decimal)Low,
            "close" => (decimal)Close,
            _ => (decimal)Close  // default fallback
        };
    }
}