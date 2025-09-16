using DotQuant.Core.Services.GraphModels;
using DotQuant.Core.Common;

namespace DotQuant.Core.Services;

public class InMemorySessionGraphProvider : ISessionGraphProvider
{
    private readonly List<PricePoint> _prices = new();
    private readonly List<SignalPoint> _signals = new();
    private readonly List<OrderPoint> _orders = new();
    private readonly object _lock = new();
    private IAccount? _account;

    // Event to notify listeners when account info changes
    public event Action<AccountInfo>? AccountChanged;

    public void AddPrice(PricePoint price)
    {
        lock (_lock)
        {
            _prices.Add(price);
        }
    }

    public void AddSignal(SignalPoint signal)
    {
        lock (_lock)
        {
            _signals.Add(signal);
        }
    }

    public void AddOrder(OrderPoint order)
    {
        lock (_lock)
        {
            _orders.Add(order);
        }
    }

    public void SetAccount(IAccount account)
    {
        lock (_lock)
        {
            _account = account;
            var info = new AccountInfo(
                Currency: account.BaseCurrency.ToString(),
                Cash: account.CashAmount.Value,
                BuyingPower: account.BuyingPower.Value,
                Equity: account.EquityAmount().Value
            );
            AccountChanged?.Invoke(info);
        }
    }

    public Task<SessionGraphData> GetGraphDataAsync()
    {
        lock (_lock)
        {
            AccountInfo? accountInfo = null;
            if (_account != null)
            {
                accountInfo = new AccountInfo(
                    Currency: _account.BaseCurrency.ToString(),
                    Cash: _account.CashAmount.Value,
                    BuyingPower: _account.BuyingPower.Value,
                    Equity: _account.EquityAmount().Value
                );
            }
            return Task.FromResult(new SessionGraphData(
                Prices: _prices.ToList(),
                Signals: _signals.ToList(),
                Orders: _orders.ToList(),
                Account: accountInfo
            ));
        }
    }
}