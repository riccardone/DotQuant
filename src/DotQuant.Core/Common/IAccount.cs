namespace DotQuant.Core.Common;

public interface IAccount
{
    Currency BaseCurrency { get; }
    DateTimeOffset LastUpdate { get; }
    Wallet Cash { get; }
    List<Order> Orders { get; }
    Dictionary<IAsset, Position> Positions { get; }
    Amount BuyingPower { get; }

    Amount CashAmount { get; }
    Amount EquityAmount();
    Wallet Equity();
    IEnumerable<IAsset> Assets { get; }
    Wallet MarketValue(params IAsset[] assets);
    Size PositionSize(IAsset asset);
    Wallet UnrealizedPNL(params IAsset[] assets);
    Amount Convert(Amount amount);
    Amount Convert(Wallet wallet);
}