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

        var sizeDecimal = size.ToDecimal();
        AveragePrice = sizeDecimal != 0
            ? costBasis.Amounts.FirstOrDefault()?.Value / sizeDecimal ?? decimal.Zero
            : decimal.Zero;

        MarketPrice = sizeDecimal != 0
            ? marketValue.Amounts.FirstOrDefault()?.Value / sizeDecimal ?? decimal.Zero
            : decimal.Zero;
    }

    // --- Legacy constructor (preserved) ---
    public Position(Size size, decimal averagePrice, decimal marketPrice)
    {
        Size = size;
        AveragePrice = averagePrice;
        MarketPrice = marketPrice;

        var value = marketPrice * size.ToDecimal();
        var cost = averagePrice * size.ToDecimal();

        MarketValue = new Wallet(new Amount(Currency.USD, value));
        CostBasis = new Wallet(new Amount(Currency.USD, cost));
        Asset = new Stock(new Symbol("UNKNOWN", "NYSE"), Currency.USD); // Placeholder
    }

    public static Position Empty(IAsset asset) =>
        new Position(asset, Size.Zero, new Wallet(), new Wallet());
}