using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using Microsoft.Extensions.Logging;

namespace DotQuant.Brokers.Trading212;

public class Trading212Broker : Broker
{
    private readonly HttpClient _http;
    private readonly ILogger<Trading212Broker> _logger;
    private readonly string _apiBaseUrl = "https://api.trading212.com";
    private readonly string _authToken;

    private IAccount _account;

    public Trading212Broker(HttpClient httpClient, ILogger<Trading212Broker> logger, string authToken)
    {
        _http = httpClient;
        _logger = logger;
        _authToken = authToken;

        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        _account = new SimulatedAccount(); // Replace with actual implementation
    }

    public override IAccount Sync(Event evt)
    {
        // Optional: Stream position/account changes on each tick
        try
        {
            // Optionally poll positions or cash balance
            var positions = _http.GetFromJsonAsync<List<Trading212Position>>($"{_apiBaseUrl}/account/positions").Result;
            _account.UpdatePositions(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing account on event");
        }

        return _account;
    }

    public override IAccount Sync()
    {
        try
        {
            var response = _http.GetAsync($"{_apiBaseUrl}/account").Result;
            if (response.IsSuccessStatusCode)
            {
                var accountJson = response.Content.ReadAsStringAsync().Result;
                var accountData = JsonSerializer.Deserialize<Trading212Account>(accountJson);
                _account.UpdateFrom(accountData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during final Sync()");
        }

        return _account;
    }

    public override void PlaceOrders(List<Order> orders)
    {
        foreach (var order in orders)
        {
            try
            {
                var payload = new
                {
                    instrument = order.Asset.Symbol, // or order.Asset.Ticker, depending on IAsset
                    quantity = Math.Abs(order.Size.Quantity),
                    price = order.Limit,
                    direction = order.Buy ? "BUY" : "SELL",
                    orderType = "LIMIT", // or "MARKET" based on additional logic
                    timeInForce = order.Tif.ToString() // assumes TIF enum maps 1:1
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = _http.PostAsync($"{_apiBaseUrl}/orders", content).Result;

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