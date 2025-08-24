using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using Microsoft.Extensions.Logging;

namespace DotQuant.Brokers.Trading212;

public class Trading212Broker : IBroker
{
    private readonly HttpClient _http;
    private readonly ILogger<Trading212Broker> _logger;
    private readonly string _apiBaseUrl;
    private readonly string _authToken;
    private IAccount _account;

    public Trading212Broker(HttpClient httpClient, ILogger<Trading212Broker> logger, string authToken, bool useDemo = true)
    {
        _http = httpClient;
        _logger = logger;
        _authToken = authToken;

        _apiBaseUrl = useDemo
            ? "https://demo.trading212.com"
            : "https://api.trading212.com";

        // Trading212 demo API requires raw token in Authorization header (no "Bearer")
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Add("Authorization", _authToken);

        _account = new SimulatedAccount(); // Replace with actual account implementation
    }

    public IAccount Sync(Event evt)
    {
        try
        {
            var url = $"{_apiBaseUrl}/api/v0/equity/positions";
            var positions = _http
                .GetFromJsonAsync<List<Trading212Position>>(url)
                .Result;

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
            var response = _http.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode)
            {
                var msg = $"Account sync failed: {response.StatusCode}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            var accountJson = response.Content.ReadAsStringAsync().Result;
            var accountData = JsonSerializer.Deserialize<Trading212Account>(accountJson);

            if (accountData == null)
            {
                throw new InvalidOperationException("Account data is null.");
            }

            _account.UpdateFrom(accountData);
            
            _logger.LogInformation(
                "Account synced: Balance={Balance:C}, FreeFunds={FreeFunds:C}, PnL={PnL:C}, Invested={Invested:C}",
                accountData.Balance, accountData.FreeFunds, accountData.PnL, accountData.Invested);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error during account Sync()");
            throw;
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
                var resp = _http.PostAsync(url, content).Result;

                if (!resp.IsSuccessStatusCode)
                {
                    var err = resp.Content.ReadAsStringAsync().Result;
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
