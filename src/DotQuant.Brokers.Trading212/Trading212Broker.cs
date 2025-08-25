using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using DotQuant.Core.Services;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DotQuant.Brokers.Trading212;

public class Trading212Broker : IBroker
{
    private readonly HttpClient _http;
    private readonly RateLimitedHttpClient _rateLimiter;
    private readonly ILogger<Trading212Broker> _logger;
    private readonly string _apiBaseUrl;
    private readonly IAccount _account;

    public Trading212Broker(HttpClient httpClient, ILogger<Trading212Broker> logger, string authToken, bool useDemo = true)
    {
        _http = httpClient;
        _logger = logger;

        _apiBaseUrl = useDemo ? "https://demo.trading212.com" : "https://api.trading212.com";
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Add("Authorization", authToken);

        _rateLimiter = new RateLimitedHttpClient(_http, _logger);
        _account = new SimulatedAccount();
    }

    public IAccount Sync(Event evt)
    {
        try
        {
            var url = $"{_apiBaseUrl}/api/v0/equity/positions";
            var positions = _rateLimiter.GetJsonWithBackoff<List<Trading212Position>>(url, "positions", 30).GetAwaiter()
                .GetResult();

            if (positions != null)
                _account.UpdatePositions(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing positions on event");
        }

        return _account;
    }

    public IAccount Sync()
    {
        try
        {
            var url = $"{_apiBaseUrl}/api/v0/equity/account/cash";
            var accountData = _rateLimiter.GetJsonWithBackoff<Trading212Account>(url, "cash", 5).GetAwaiter()
                .GetResult();

            if (accountData == null)
                throw new InvalidOperationException("Account data fetch failed.");

            _account.UpdateFrom(accountData);
            _logger.LogInformation("Synced account: Balance={Balance}, FreeFunds={FreeFunds}", accountData.Balance, accountData.FreeFunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Sync()");
        }

        return _account;
    }

    public void PlaceOrders(List<Order> orders)
    {
        foreach (var order in orders)
        {
            try
            {
                var payload = new
                {
                    instrument = order.Asset.Symbol,
                    quantity = Math.Abs(order.Size.Quantity),
                    price = order.Limit,
                    direction = order.Buy ? "BUY" : "SELL",
                    orderType = "LIMIT", // or "MARKET"
                    timeInForce = order.Tif.ToString()
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_apiBaseUrl}/api/v0/equity/orders";
                var resp = _http.PostAsync(url, content).GetAwaiter().GetResult();

                if (!resp.IsSuccessStatusCode)
                {
                    var err = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    _logger.LogWarning("Order failed: {Order} => {Error}", order, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order placement failed for {Order}", order);
            }
        }
    }
}
