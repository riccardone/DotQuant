namespace DotQuant.Core.Common;

public class SimulatedAccount : IAccount
{
    public Currency BaseCurrency { get; private set; } = Currency.USD;

    public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.UtcNow;

    public Wallet Cash { get; set; } = new();

    public List<Order> Orders { get; } = new();

    public Dictionary<IAsset, Position> Positions { get; } = new();

    public Amount BuyingPower => Cash.Total(BaseCurrency, LastUpdate); // Simplified logic

    public Amount CashAmount => Cash.Total(BaseCurrency, LastUpdate);

    public Amount EquityAmount()
    {
        var market = MarketValue().Total(BaseCurrency, LastUpdate);
        return CashAmount + market;
    }

    public Wallet Equity()
    {
        return Cash + MarketValue(); // Includes both cash and holdings
    }

    public IEnumerable<IAsset> Assets => Positions.Keys;

    public Wallet MarketValue(params IAsset[] assets)
    {
        if (assets is { Length: > 0 })
        {
            var filtered = Positions
                .Where(kv => assets.Contains(kv.Key))
                .Select(p => p.Value.MarketValue);
            return filtered.Aggregate(new Wallet(), (acc, w) => acc + w);
        }
        else
        {
            return Positions.Values
                .Select(p => p.MarketValue)
                .Aggregate(new Wallet(), (acc, w) => acc + w);
        }
    }

    public Wallet UnrealizedPNL(params IAsset[] assets)
    {
        var relevant = assets.Length > 0
            ? Positions.Where(kv => assets.Contains(kv.Key)).Select(kv => kv.Value)
            : Positions.Values;

        var pnl = new Wallet();
        foreach (var pos in relevant)
        {
            foreach (var amt in pos.MarketValue.Amounts)
            {
                var cost = pos.CostBasis.Amounts.FirstOrDefault(a => a.Currency == amt.Currency);
                var pnlValue = amt.Value - (cost?.Value ?? 0m);
                pnl.Deposit(new Amount(amt.Currency, pnlValue));
            }
        }

        return pnl;
    }

    public Size PositionSize(IAsset asset)
    {
        return Positions.TryGetValue(asset, out var pos) ? pos.Size : Size.Zero;
    }

    public Amount Convert(Amount amount)
    {
        // Stub: 1:1 conversion
        return new Amount(BaseCurrency, amount.Value);
    }

    public Amount Convert(Wallet wallet)
    {
        return wallet.Total(BaseCurrency, LastUpdate);
    }
}
