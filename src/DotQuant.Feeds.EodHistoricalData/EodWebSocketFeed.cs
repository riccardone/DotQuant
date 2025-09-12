using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using DotQuant.Core.Extensions;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodWebSocketFeed : LiveFeed
{
    private readonly ILogger<EodWebSocketFeed> _logger;
    private readonly string _apiKey;
    private readonly string[] _symbols;
    private ClientWebSocket? _webSocket;
    private CancellationToken _ct;
    private readonly ConcurrentDictionary<string, DateTime> _lastSeen = new();
    private readonly IConfiguration _config;

    public EodWebSocketFeed(string apiKey, string[] symbols, ILogger<EodWebSocketFeed> logger, IConfiguration config, IMarketStatusService marketStatusService)
    {
        _apiKey = apiKey;
        _symbols = symbols;
        _logger = logger;
        _config = config;
        EnableMarketStatus(marketStatusService, logger);
    }

    public override async Task PlayAsync(ChannelWriter<Event> channel, CancellationToken ct)
    {
        _ct = ct;
        var reconnectDelay = TimeSpan.FromSeconds(5);

        while (!_ct.IsCancellationRequested)
        {
            try
            {
                // Check if at least one market is open before subscribing
                var openSymbols = new List<string>();
                foreach (var symbolStr in _symbols)
                {
                    var parts = symbolStr.Split('.');
                    if (parts.Length != 2) continue;
                    var symbol = new Symbol(parts[0], parts[1]);
                    if (await IsMarketOpenAsync(symbol, _ct))
                        openSymbols.Add(symbolStr);
                }

                if (openSymbols.Count == 0)
                {
                    _logger.LogInformation("All markets are closed for the requested symbols. Will retry in {Delay} seconds...", reconnectDelay.TotalSeconds);
                    await Task.Delay(reconnectDelay, ct);
                    continue;
                }

                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();

                var uri = new Uri($"ws://ws.eodhistoricaldata.com/ws?api_token={_apiKey}");

                await _webSocket.ConnectAsync(uri, _ct);
                _logger.LogInformation("Connected to EOD WebSocket");

                await SubscribeToSymbolsAsync(openSymbols);
                await ReceiveLoop(channel);

                if (_webSocket.CloseStatus.HasValue)
                {
                    _logger.LogWarning("WebSocket closed: {CloseStatus} - {Description}",
                        _webSocket.CloseStatus, _webSocket.CloseStatusDescription);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket connection failed. Retrying in {Delay} seconds...", reconnectDelay.TotalSeconds);
                await Task.Delay(reconnectDelay, ct);
                reconnectDelay = TimeSpan.FromSeconds(Math.Min(reconnectDelay.TotalSeconds * 2, 60));
            }
        }
    }

    private async Task SubscribeToSymbolsAsync(List<string> symbols)
    {
        var msg = new
        {
            action = "subscribe",
            symbols = string.Join(",", symbols)
        };

        var json = JsonSerializer.Serialize(msg);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket?.SendAsync(bytes, WebSocketMessageType.Text, true, _ct);
        _logger.LogInformation("Subscribed to symbols: {Symbols}", msg.symbols);
    }

    private async Task ReceiveLoop(ChannelWriter<Event> channel)
    {
        while (!_ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
        {
            try
            {
                var json = await ReadFullMessage();
                if (string.IsNullOrWhiteSpace(json)) continue;

                RealTimeTick? tick = null;
                try
                {
                    tick = JsonSerializer.Deserialize<RealTimeTick>(json);
                }
                catch (Exception)
                {
                    // Ignore, will try error parsing below
                }

                if (tick != null && tick.IsValid())
                {
                    if (_lastSeen.TryGetValue(tick.Symbol, out var last) && last == tick.TimeUtc)
                        continue;

                    _lastSeen[tick.Symbol] = tick.TimeUtc;

                    var parts = tick.Symbol.Split('.');
                    var symbol = new Symbol(parts[0], parts[1]);
                    if (!await IsMarketOpenAsync(symbol, _ct))
                        continue;

                    var evt = ConvertToEvent(tick);
                    await SendAsync(evt);
                    continue;
                }

                // Try to parse as error
                try
                {
                    var error = JsonSerializer.Deserialize<WebSocketError>(json);
                    if (error != null && error.StatusCode != 0)
                    {
                        _logger.LogError("WebSocket error received: {StatusCode} - {Message}", error.StatusCode, error.Message);
                        continue;
                    }
                }
                catch (Exception)
                {
                    // Ignore, will log raw below
                }

                // If not tick or error, log raw message
                _logger.LogWarning("Unrecognized WebSocket message: {Message}", json);
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocketException encountered in ReceiveLoop. Will reconnect.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process tick message");
            }
        }

        _logger.LogWarning("WebSocket closed or cancelled. Exiting receive loop.");
    }

    private async Task<string> ReadFullMessage()
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket.ReceiveAsync(buffer, _ct);
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private Event ConvertToEvent(RealTimeTick tick)
    {
        var parts = tick.Symbol.Split('.');
        var symbol = new Symbol(parts[0], parts[1]);

        var currency = _config.ResolveCurrency(symbol, _logger);
        var asset = new Stock(symbol, currency);

        return new Event(tick.TimeUtc, new List<PriceItem>
        {
            new PriceItem(asset, tick.Price, tick.Price, tick.Price, tick.Price, tick.Volume ?? 0, TimeSpan.FromSeconds(1))
        });
    }

    private class RealTimeTick
    {
        [JsonPropertyName("s")] public string Symbol { get; set; }
        [JsonPropertyName("p")] public decimal Price { get; set; }
        [JsonPropertyName("v")] public long? Volume { get; set; }
        [JsonPropertyName("t")] public DateTime TimeUtc { get; set; }

        public bool IsValid() => !string.IsNullOrEmpty(Symbol) && Price > 0 && TimeUtc > DateTime.MinValue;
    }

    private class WebSocketError
    {
        [JsonPropertyName("status_code")] public int StatusCode { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; }
    }
}