using System.Globalization;
using DotQuant.Core.Common;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Brokers;

public class SimBroker : Broker
{
    private readonly ILogger<SimBroker> _logger;
    private readonly Wallet _initialDeposit;
    private readonly Currency _baseCurrency;
    private readonly IAccountModel _accountModel;
    private readonly Dictionary<string, DateTime> _orderEntry;
    private readonly TimeZoneInfo _exchangeTimeZone;
    private readonly List<Order> _pendingOrders;
    private readonly InternalAccount _account;
    private int _nextOrderId;

    public SimBroker(
        ILogger<SimBroker> logger,
        Wallet? initialDeposit = null,
        Currency? baseCurrency = null,
        IAccountModel? accountModel = null,
        TimeZoneInfo? exchangeTimeZone = null)
    {
        _logger = logger;
        _initialDeposit = initialDeposit ?? new Wallet(new Amount(Currency.USD, 1_000_000m));
        _baseCurrency = baseCurrency ?? _initialDeposit.Currencies.FirstOrDefault() ?? Currency.USD;
        _accountModel = accountModel ?? new CashAccount();
        _orderEntry = new Dictionary<string, DateTime>();
        _exchangeTimeZone = exchangeTimeZone ?? TimeZoneInfo.Utc;
        _pendingOrders = new List<Order>();
        _account = new InternalAccount(_baseCurrency);
        _nextOrderId = 0;

        Reset();
    }

    public SimBroker(ILogger<SimBroker> logger, decimal deposit, string currencyCode = "USD")
        : this(logger, new Wallet(new Amount(Currency.GetInstance(currencyCode), deposit))) { }

    public override IAccount Sync(Event? e = null)
    {
        lock (this)
        {
            foreach (var order in _pendingOrders)
            {
                if (order.IsCancellation())
                {
                    _account.Orders.RemoveAll(o => o.Id == order.Id);
                }
                else if (order.IsModify())
                {
                    var removed = _account.Orders.RemoveAll(o => o.Id == order.Id) > 0;
                    if (removed) _account.Orders.Add(order);
                }
                else
                {
                    if (string.IsNullOrEmpty(order.Id))
                        order.Id = (_nextOrderId++).ToString(CultureInfo.InvariantCulture);
                    _account.Orders.Add(order);
                }
            }

            _pendingOrders.Clear();

            if (e != null)
            {
                SimulateMarket(e);
                _account.UpdateMarketPrices(e);
                _account.LastUpdate = e.Time;
                _accountModel.UpdateAccount(_account);
            }

            return _account.ToAccount();
        }
    }

    public override IAccount Sync() => _account.ToAccount();

    public override void PlaceOrders(List<Order> orders)
    {
        lock (this)
        {
            _pendingOrders.AddRange(orders);
        }
    }

    private void Reset()
    {
        _account.Clear();
        _account.Cash.Deposit(_initialDeposit);
        _accountModel.UpdateAccount(_account);
    }

    private void SimulateMarket(Event e)
    {
        var time = e.Time;
        var prices = e.Prices;
        var orders = _account.Orders.ToList();

        if (orders.Count == 0) return; // Skip if no active orders

        _logger.LogInformation("[MARKET] {Time:u} | Prices: {Count} | Orders: {OrderCount}", time, prices.Count, orders.Count);

        foreach (var order in orders)
        {
            if (IsExpired(order, time.DateTime))
            {
                _logger.LogDebug("[EXPIRE] Order {Id} for {Asset} expired.", order.Id, order.Asset.Symbol);
                DeleteOrder(order);
                continue;
            }

            var priceOpt = e.GetPrice(order.Asset);
            if (priceOpt != null && order.IsExecutable(priceOpt.Value))
            {
                UpdatePosition(order.Asset, order.Size, priceOpt.Value);

                var value = order.Asset.Value(order.Size, priceOpt.Value);
                _account.Cash.Withdraw(value);
                _logger.LogInformation("[EXECUTE] {Asset} {Qty} @ {Price}", order.Asset.Symbol, order.Size.Quantity, priceOpt.Value);
                DeleteOrder(order);
            }
            else
            {
                _logger.LogDebug("[SKIP] Order {Id} for {Asset} not executable at price: {Price}", order.Id, order.Asset.Symbol, priceOpt);
            }
        }
    }

    private bool IsExpired(Order order, DateTime time)
    {
        if (order.Tif == TIF.GTC) return false;

        if (_orderEntry.TryGetValue(order.Id, out var entryDate))
        {
            var currentDate = TimeZoneInfo.ConvertTime(time, _exchangeTimeZone).Date;
            return currentDate > entryDate;
        }

        _orderEntry[order.Id] = TimeZoneInfo.ConvertTime(time, _exchangeTimeZone).Date;
        return false;
    }

    private void DeleteOrder(Order order)
    {
        _account.Orders.RemoveAll(o => o.Id == order.Id);
    }

    private void UpdatePosition(IAsset asset, Size size, decimal price)
    {
        if (!_account.Positions.TryGetValue(asset, out var position))
        {
            _account.Positions[asset] = new Position(size, price, price);
            return;
        }

        var newQty = position.Size.Quantity + size.Quantity;
        if (newQty == 0)
        {
            _account.Positions.Remove(asset);
            return;
        }

        decimal newAvgPrice;
        if (Math.Sign(position.Size.Quantity) == Math.Sign(size.Quantity))
        {
            var totalQty = position.Size.Quantity + size.Quantity;
            newAvgPrice = (position.Size.Quantity * position.AveragePrice + size.Quantity * price) / totalQty;
        }
        else
        {
            newAvgPrice = price; // Reversal or reduction resets avg price
        }

        _account.Positions[asset] = new Position(new Size(newQty), newAvgPrice, price);
    }
}
