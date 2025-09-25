using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using IBApi;
using Microsoft.Extensions.Logging;
using Order = DotQuant.Core.Common.Order;

namespace DotQuant.Brokers.IBKR;

public interface IIBrokerClient
{
    Task ConnectAsync();
    Task DisconnectAsync();
}

public class IBKRBroker : IBroker, IIBrokerClient
{
    private readonly ILogger<IBKRBroker> _logger;
    private readonly IBKRConfig _config;
    private readonly EReaderMonitorSignal _signal;
    private readonly EClientSocket _client;
    private IAccount _account = new InternalAccount(Currency.USD); // TODO why here USD?
    private readonly string _accountId;
    private int _nextOrderId = 0;

    public bool IsConnected => _client.IsConnected();

    public IBKRBroker(ILogger<IBKRBroker> logger, IBKRConfig config)
    {
        _logger = logger;
        _config = config;
        _signal = new EReaderMonitorSignal();
        _client = new EClientSocket(new DefaultEWrapper(), _signal);
        _accountId = config.Account;
    }

    public async Task ConnectAsync()
    {
        _client.eConnect(_config.Host, _config.Port, _config.Client);
        _logger.LogInformation("Connecting to IBKR at {Host}:{Port} with clientId={ClientId}", _config.Host, _config.Port, _config.Client);

        var reader = new EReader(_client, _signal);
        reader.Start();
        
        await Task.Run(() =>
        {
            while (_client.IsConnected())
            {
                _signal.waitForSignal();
                reader.processMsgs();
            }
        });
    }

    public Task DisconnectAsync()
    {
        if (_client.IsConnected())
        {
            _client.eDisconnect();
            _logger.LogInformation("Disconnected from IBKR.");
        }
        return Task.CompletedTask;
    }

    public IAccount Sync(Event evt)
    {
        if (evt != null)
        {
            if (evt.Time < DateTimeOffset.UtcNow - TimeSpan.FromHours(1))
                throw new NotSupportedException("Cannot place orders in the past");
        }

        var tags = $"{AccountSummaryTags.BuyingPower}, {AccountSummaryTags.TotalCashValue}";

        _client.reqPositions();
        _client.reqAllOpenOrders();
        _client.reqAccountSummary(1, "All", tags);

        return _account;
    }

    public IAccount Sync()
    {
        // TODO
        return _account;
    }

    public void PlaceOrders(List<Order> orders)
    {
        foreach (var order in orders)
        {
            _logger.LogInformation("received order={AssetSymbol}", order.Asset.Symbol);
            if (order.IsCancellation())
                CancelOrder(order);
            else
            {
                order.Id = _nextOrderId++.ToString();
                PlaceOrder(order);
            }
        }
    }

    private void CancelOrder(Order order)
    {
        var id = order.Id;
        _logger.LogInformation("cancelling order with id {Id}", id);
        _client.cancelOrder(int.Parse(id), new OrderCancel());
    }

    private void PlaceOrder(Order order)
    {
        var contract = order.Asset.ToContract();
        var ibOrder = CreateIbOrder(order);
        // ibOrder.log(contract) // ??
        var id = ibOrder.OrderId;
        _client.placeOrder(id, contract, ibOrder);
        _account.Orders.Add(order);
    }

    private IBApi.Order CreateIbOrder(Order order)
    {
        var result = new IBApi.Order
        {
            OrderType = "LMT",
            LmtPrice = decimal.ToDouble(order.Limit)
        };

        var action = order.Buy ? "BUY" : "SELL";
        result.Action = action;
        result.TotalQuantity = order.Size.Quantity;
        if (!string.IsNullOrWhiteSpace(_accountId))
            result.Account = _accountId;
        if (string.IsNullOrWhiteSpace(order.Id))
            order.Id = _nextOrderId++.ToString();
        result.OrderId = int.Parse(order.Id);
        // orderIds.add(order.id.toInt()) ??

        return result;
    }
}
