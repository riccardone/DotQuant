namespace DotQuant.Core.Common;

public abstract class AccountBase : IAccount
{
    public abstract Currency BaseCurrency { get; }
    public abstract DateTimeOffset LastUpdate { get; set; }
    public abstract Wallet Cash { get; }
    public abstract List<Order> Orders { get; }
    public abstract Dictionary<IAsset, Position> Positions { get; }
    public abstract Amount BuyingPower { get; set; }

    public Amount CashAmount => Convert(Cash);

    public Amount EquityAmount() => Convert(Equity());

    public Wallet Equity() => Cash + MarketValue();

    public IEnumerable<IAsset> Assets => Positions.Keys;

    public Wallet MarketValue(params IAsset[] assets)
    {
        var filtered = Positions.Where(p => assets.Length == 0 || assets.Contains(p.Key));
        var result = new Wallet(new Amount(BaseCurrency, 0.0m));

        foreach (var (asset, position) in filtered)
        {
            var qty = position.Size.Quantity;
            var totalValue = qty * position.MarketPrice;
            var amount = new Amount(BaseCurrency, totalValue);
            result.Deposit(amount);
        }

        return result;
    }

    public Size PositionSize(IAsset asset) =>
        Positions.TryGetValue(asset, out var pos) ? pos.Size : new Size(0);

    public Wallet UnrealizedPNL(params IAsset[] assets)
    {
        var filtered = Positions.Where(p => assets.Length == 0 || assets.Contains(p.Key));
        var result = new Wallet(new Amount(BaseCurrency, 0.0m));

        foreach (var (asset, position) in filtered)
        {
            var qty = position.Size.Quantity;
            var pnlValue = qty * (position.MarketPrice - position.AveragePrice);
            var amount = new Amount(BaseCurrency, pnlValue);
            result.Deposit(amount);
        }

        return result;
    }

    public abstract Amount Convert(Amount amount);
    public abstract Amount Convert(Wallet wallet);
}