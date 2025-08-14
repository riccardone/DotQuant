using System.Text;

namespace DotQuant.Core.Common;

/// <summary>
/// Internal account used by brokers (e.g., FakeBroker).
/// Only brokers hold a reference; they share a snapshot externally via Account.
/// </summary>
public class InternalAccount : AccountBase
{
    public override Currency BaseCurrency { get; }
    public override DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.MinValue;
    public override List<Order> Orders { get; } = new();
    public override Wallet Cash { get; } = new Wallet();
    public override Amount BuyingPower { get; set; } 
    public override Dictionary<IAsset, Position> Positions { get; } = new();

    public InternalAccount(Currency baseCurrency)
    {
        BaseCurrency = baseCurrency;
        BuyingPower = new Amount(baseCurrency, 0.0m);
    }

    public void Clear()
    {
        lock (this)
        {
            LastUpdate = DateTimeOffset.MinValue;
            Orders.Clear();
            Positions.Clear();
            Cash.Clear();
        }
    }

    public void DeleteOrder(Order order)
    {
        Orders.RemoveAll(o => o.Id == order.Id);
    }

    public void SetPosition(IAsset asset, Position position)
    {
        lock (this)
        {
            if (position.Closed)
            {
                Positions.Remove(asset);
            }
            else
            {
                Positions[asset] = position;
            }
        }
    }

    public void UpdateMarketPrices(Event e, string priceType = "DEFAULT")
    {
        if (Positions.Count == 0) return;

        var prices = e.Prices;
        foreach (var (asset, position) in Positions.ToList())
        {
            if (prices.TryGetValue(asset, out var priceItem))
            {
                var price = priceItem.GetPrice(priceType);
                var newPosition = position with { MarketPrice = price };
                Positions[asset] = newPosition;
            }
        }
    }

    /// <summary>
    /// Create an immutable Account snapshot (currently returns self since we assume read-only).
    /// </summary>
    public IAccount ToAccount()
    {
        lock (this)
        {
            return this;
        }
    }

    public override string ToString()
    {
        var pString = string.Join(", ", Positions.Select(p => $"{p.Value.Size}@{p.Key.Symbol}"));
        var oString = string.Join(", ", Orders.Select(o => $"{o.Size}@{o.Asset.Symbol}"));

        var sb = new StringBuilder();
        sb.AppendLine($"last update  : {LastUpdate}");
        sb.AppendLine($"cash         : {Cash}");
        sb.AppendLine($"buying Power : {BuyingPower}");
        sb.AppendLine($"equity       : {Equity()}");
        sb.AppendLine($"positions    : {pString}");
        sb.AppendLine($"open orders  : {oString}");

        return sb.ToString();
    }

    public double Equity()
    {
        var positionsValue = Positions.Sum(p => p.Value.MarketPrice * p.Value.Size.Quantity);
        var cashValue = Cash.Total(BaseCurrency, LastUpdate).Value;
        return (double)(cashValue + positionsValue);
    }

    public override Amount Convert(Amount amount)
    {
        // TODO: Implement FX conversion logic
        if (amount.Currency == BaseCurrency) 
            return amount;
        return new Amount(BaseCurrency, 0.0m); // Placeholder
    }

    public override Amount Convert(Wallet wallet)
    {
        var total = new Amount(BaseCurrency, 0.0m);
        foreach (var amount in wallet.Amounts)
        {
            total = total + Convert(amount);
        }
        return total;
    }
}