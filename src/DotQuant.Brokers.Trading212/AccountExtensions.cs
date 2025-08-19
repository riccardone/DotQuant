using DotQuant.Core.Common;

namespace DotQuant.Brokers.Trading212;

public static class AccountExtensions
{
    public static void UpdateFrom(this IAccount account, Trading212Account t212)
    {
        if (t212 == null || account is not SimulatedAccount sim)
            return;

        var currency = new Currency(t212.Currency);
        var amount = new Amount(currency, t212.FreeFunds);

        sim.Cash = new Wallet(amount);
        sim.LastUpdate = DateTimeOffset.UtcNow;
    }

    public static void UpdatePositions(this IAccount account, List<Trading212Position> positions)
    {
        if (positions == null || account is not SimulatedAccount sim)
            return;

        sim.Positions.Clear();

        foreach (var p in positions)
        {
            var currency = new Currency(p.Currency);
            var asset = new Stock(p.Ticker, currency);
            var size = new Size(p.Quantity);

            var marketValue = new Wallet(new Amount(currency, p.CurrentPrice * p.Quantity));
            var costBasis = new Wallet(new Amount(currency, p.AveragePrice * p.Quantity));

            var position = new Position(asset, size, marketValue, costBasis);
            sim.Positions[asset] = position;
        }

        sim.LastUpdate = DateTimeOffset.UtcNow;
    }
}