namespace DotQuant.Core.Common;

public record Position
{
    public IAsset Asset { get; init; }
    public Size Size { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal MarketPrice { get; init; }
    public Wallet MarketValue { get; init; }
    public Wallet CostBasis { get; init; }

    public bool Closed => Size.IsZero;
    public bool Open => !Size.IsZero;
    public bool Long => Size.Quantity > 0;

    // --- New constructor (preferred for brokers) ---
    public Position(IAsset asset, Size size, Wallet marketValue, Wallet costBasis)
    {
        Asset = asset;
        Size = size;
        MarketValue = marketValue;
        CostBasis = costBasis;
        AveragePrice = costBasis.Amounts.FirstOrDefault()?.Value / size.ToDecimal() ?? 0.0m;
        MarketPrice = marketValue.Amounts.FirstOrDefault()?.Value / size.ToDecimal() ?? 0.0m;
    }

    // --- Legacy constructor (preserved) ---
    public Position(Size size, decimal averagePrice, decimal marketPrice)
    {
        Size = size;
        AveragePrice = averagePrice;
        MarketPrice = marketPrice;
        MarketValue = new Wallet(new Amount(Currency.USD, marketPrice * size.ToDecimal())); // USD default
        CostBasis = new Wallet(new Amount(Currency.USD, averagePrice * size.ToDecimal()));
        Asset = new Stock("UNKNOWN", Currency.USD); // Placeholder for compatibility
    }

    public static Position Empty(IAsset asset) =>
        new Position(asset, Size.Zero, new Wallet(), new Wallet());
}