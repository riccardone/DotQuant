using DotQuant.Core.Common;
using DotQuant.Core.Extensions;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodHistoricalDataFeed : IFeed
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly string[] _symbols;
    private readonly string _apiKey;
    private readonly TimeSpan _interval;
    private readonly IEnumerable<IFeed> _fallbackFeeds;
    private readonly DateTime _start;
    private readonly DateTime _end;
    private readonly bool _isLive;
    private readonly ConcurrentDictionary<string, bool> _fallbackLaunched = new();

    public EodHistoricalDataFeed(
        ILogger logger,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        string apiKey,
        string[] symbols,
        TimeSpan? interval = null,
        IEnumerable<IFeed>? fallbackFeeds = null,
        DateTime? start = null,
        DateTime? end = null,
        bool isLive = true)
    {
        _logger = logger;
        _httpFactory = httpFactory;
        _config = config;
        _apiKey = apiKey;
        _symbols = symbols;
        _interval = interval ?? TimeSpan.FromSeconds(30);
        _fallbackFeeds = fallbackFeeds ?? [];
        _start = start ?? DateTime.UtcNow;
        _end = end ?? DateTime.UtcNow;
        _isLive = isLive;
    }

    public async Task PlayAsync(ChannelWriter<Event> writer, CancellationToken cancellationToken)
    {
        var client = _httpFactory.CreateClient();

        if (_isLive)
        {
            await RunLiveMode(writer, client, cancellationToken);
        }
        else
        {
            await RunHistoricalMode(writer, client, cancellationToken);
            writer.TryComplete(); // Close writer after backtest
        }
    }

    private async Task RunLiveMode(ChannelWriter<Event> writer, HttpClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var symbolStr in _symbols)
            {
                try
                {
                    var adjustedSymbol = MapSymbol(symbolStr);
                    var url = $"https://eodhistoricaldata.com/api/intraday/{adjustedSymbol}?interval=5m&range=1d&api_token={_apiKey}&fmt=json";
                    var response = await client.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var bars = JsonSerializer.Deserialize<List<IntradayBar>>(json);
                    var latest = bars?.LastOrDefault();

                    if (latest == null)
                    {
                        _logger.LogWarning("No intraday bars for {Symbol}", symbolStr);
                        continue;
                    }

                    await EmitPrice(writer, symbolStr, latest, _interval, DateTime.UtcNow, cancellationToken);
                }
                catch (Exception ex)
                {
                    await HandleFallback(symbolStr, writer, cancellationToken, ex);
                }
            }

            await Task.Delay(_interval, cancellationToken);
        }
    }

    private async Task RunHistoricalMode(ChannelWriter<Event> writer, HttpClient client, CancellationToken cancellationToken)
    {
        foreach (var symbolStr in _symbols)
        {
            try
            {
                var adjustedSymbol = MapSymbol(symbolStr);
                var from = _start.ToString("yyyy-MM-dd");
                var to = _end.ToString("yyyy-MM-dd");

                var url = $"https://eodhistoricaldata.com/api/eod/{adjustedSymbol}?from={from}&to={to}&api_token={_apiKey}&fmt=json";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var bars = JsonSerializer.Deserialize<List<IntradayBar>>(json);

                if (bars == null || bars.Count == 0)
                {
                    _logger.LogWarning("No historical data returned for {Symbol}", symbolStr);
                    continue;
                }

                foreach (var bar in bars)
                {
                    await EmitPrice(writer, symbolStr, bar, TimeSpan.FromDays(1), bar.Datetime, cancellationToken);
                }

                _logger.LogInformation("Historical data for {Symbol} emitted: {Count} bars", symbolStr, bars.Count);
            }
            catch (Exception ex)
            {
                await HandleFallback(symbolStr, writer, cancellationToken, ex);
            }
        }
    }

    private async Task EmitPrice(ChannelWriter<Event> writer, string symbolStr, IntradayBar bar, TimeSpan timespan, DateTime time, CancellationToken cancellationToken)
    {
        var parts = symbolStr.Split('.');
        if (parts.Length != 2)
            throw new FormatException($"Invalid symbol format: {symbolStr}. Expected format 'TICKER.EXCHANGE'");

        var symbol = new Symbol(parts[0], parts[1]);
        var currency = _config.ResolveCurrency(symbol, _logger);
        var asset = new Stock(symbol, currency);

        var evt = new Event(DateTime.SpecifyKind(time, DateTimeKind.Utc), new List<PriceItem>
        {
            new PriceItem(asset, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, timespan)
        });

        await writer.WriteAsync(evt, cancellationToken);
    }

    private async Task HandleFallback(string symbol, ChannelWriter<Event> writer, CancellationToken cancellationToken, Exception ex)
    {
        _logger.LogWarning(ex, "Primary EOD feed failed for {symbol}", symbol);

        if (_fallbackLaunched.ContainsKey(symbol))
        {
            _logger.LogInformation("Fallback already launched for {symbol}", symbol);
            return;
        }

        foreach (var fallback in _fallbackFeeds)
        {
            try
            {
                _logger.LogInformation("Launching fallback for {symbol} via {feed}", symbol, fallback.GetType().Name);
                _ = Task.Run(() => fallback.PlayAsync(writer, cancellationToken), cancellationToken);
                _fallbackLaunched.TryAdd(symbol, true);
                return;
            }
            catch (Exception fbEx)
            {
                _logger.LogWarning(fbEx, "Failed to launch fallback {feed} for {symbol}", fallback.GetType().Name, symbol);
            }
        }

        _logger.LogError("All fallback feeds failed for {symbol}", symbol);
    }

    private static string MapSymbol(string symbol)
    {
        return symbol
            .Replace(".NASDAQ", ".US", StringComparison.OrdinalIgnoreCase)
            .Replace(".NYSE", ".US", StringComparison.OrdinalIgnoreCase)
            .Replace(".MTA", ".MI", StringComparison.OrdinalIgnoreCase); // correct for EOD
    }

    private class IntradayBar
    {
        [JsonPropertyName("datetime")]
        public DateTime Datetime { get; set; }

        [JsonPropertyName("open")]
        public decimal Open { get; set; }

        [JsonPropertyName("high")]
        public decimal High { get; set; }

        [JsonPropertyName("low")]
        public decimal Low { get; set; }

        [JsonPropertyName("close")]
        public decimal Close { get; set; }

        [JsonPropertyName("volume")]
        public long Volume { get; set; }
    }
}
