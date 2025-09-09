using DotQuant.Core.Common;
using DotQuant.Core.Extensions;
using DotQuant.Core.Feeds;
using DotQuant.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodHistoricalDataFeed : LiveFeed
{
    private readonly ILogger<EodHistoricalDataFeed> _logger;
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
    private JsonSerializerOptions _options;

    public EodHistoricalDataFeed(
        ILogger<EodHistoricalDataFeed> logger,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        string apiKey,
        string[] symbols,
        IMarketStatusService marketStatusService,
        TimeSpan? interval = null,
        IEnumerable<IFeed>? fallbackFeeds = null,
        DateTime? start = null,
        DateTime? end = null,
        bool isLive = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
        _interval = interval ?? TimeSpan.FromSeconds(30);
        _fallbackFeeds = fallbackFeeds ?? [];
        _start = start ?? DateTime.UtcNow;
        _end = end ?? DateTime.UtcNow;
        _isLive = isLive;

        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _options.Converters.Add(new SafeDateTimeConverter());

        EnableMarketStatus(marketStatusService, logger);
    }

    public override async Task PlayAsync(ChannelWriter<Event> channel, CancellationToken cancellationToken)
    {
        var client = _httpFactory.CreateClient();

        if (_isLive)
        {
            await RunLiveMode(channel, client, cancellationToken);
        }
        else
        {
            await RunHistoricalMode(channel, client, cancellationToken);
            channel.TryComplete(); // Close writer after backtest
        }
    }

    private async Task RunLiveMode(ChannelWriter<Event> channel, HttpClient client, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var symbolStr in _symbols)
            {
                try
                {
                    var parts = symbolStr.Split('.');
                    if (parts.Length != 2)
                    {
                        _logger.LogWarning("Invalid symbol format: {Symbol}", symbolStr);
                        continue;
                    }

                    var symbol = new Symbol(parts[0], parts[1]);
                    if (!await IsMarketOpenAsync(symbol, ct))
                        continue;

                    var adjustedSymbol = MapSymbol(symbolStr);
                    var url = $"https://eodhistoricaldata.com/api/intraday/{adjustedSymbol}?interval=5m&range=1d&api_token={_apiKey}&fmt=json";

                    var response = await client.GetAsync(url, ct);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(ct);
                    var bars = JsonSerializer.Deserialize<List<IntradayBar>>(json, _options);
                    var latest = bars?.LastOrDefault();

                    if (latest == null)
                    {
                        _logger.LogWarning("No intraday bars for {Symbol}", symbolStr);
                        continue;
                    }

                    await EmitPrice(channel, symbol, latest, _interval, DateTime.UtcNow, ct);
                }
                catch (Exception ex)
                {
                    await HandleFallback(symbolStr, channel, ct, ex);
                }
            }

            await Task.Delay(_interval, ct);
        }
    }

    private async Task RunHistoricalMode(ChannelWriter<Event> channel, HttpClient client, CancellationToken ct)
    {
        foreach (var symbolStr in _symbols)
        {
            try
            {
                var parts = symbolStr.Split('.');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid symbol format: {Symbol}", symbolStr);
                    continue;
                }

                var symbol = new Symbol(parts[0], parts[1]);
                var adjustedSymbol = MapSymbol(symbolStr);
                var from = _start.ToString("yyyy-MM-dd");
                var to = _end.ToString("yyyy-MM-dd");

                var url = $"https://eodhistoricaldata.com/api/eod/{adjustedSymbol}?from={from}&to={to}&api_token={_apiKey}&fmt=json";

                var response = await client.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var bars = JsonSerializer.Deserialize<List<IntradayBar>>(json, _options);

                if (bars == null || bars.Count == 0)
                {
                    _logger.LogWarning("No historical data returned for {Symbol}", symbolStr);
                    continue;
                }

                foreach (var bar in bars)
                {
                    await EmitPrice(channel, symbol, bar, TimeSpan.FromDays(1), bar.Datetime, ct);
                }

                _logger.LogInformation("Historical data for {Symbol} emitted: {Count} bars", symbolStr, bars.Count);
            }
            catch (Exception ex)
            {
                await HandleFallback(symbolStr, channel, ct, ex);
            }
        }
    }

    private async Task EmitPrice(ChannelWriter<Event> channel, Symbol symbol, IntradayBar bar, TimeSpan timespan, DateTime time, CancellationToken ct)
    {
        var currency = _config.ResolveCurrency(symbol, _logger);
        var asset = new Stock(symbol, currency);

        if (!bar.IsValidPrice)
        {
            _logger.LogDebug("Invalid price data for {Symbol} at {Time}", symbol, bar.Datetime);
            return;
        }
        var evt = new Event(DateTime.SpecifyKind(time, DateTimeKind.Utc), new List<PriceItem>
        {
            new PriceItem(asset, bar.Open.Value, bar.High.Value, bar.Low.Value, bar.Close.Value, bar.Volume ?? 0, timespan)
        });

        await SendAsync(evt);
    }

    private async Task HandleFallback(string symbol, ChannelWriter<Event> channel, CancellationToken ct, Exception ex)
    {
        _logger.LogWarning(ex, "Primary EOD feed failed for {Symbol}", symbol);

        if (_fallbackLaunched.ContainsKey(symbol))
        {
            _logger.LogInformation("Fallback already launched for {Symbol}", symbol);
            return;
        }

        foreach (var fallback in _fallbackFeeds)
        {
            try
            {
                _logger.LogInformation("Launching fallback for {Symbol} via {Feed}", symbol, fallback.GetType().Name);
                _ = Task.Run(() => fallback.PlayAsync(channel, ct), ct);
                _fallbackLaunched.TryAdd(symbol, true);
                return;
            }
            catch (Exception fbEx)
            {
                _logger.LogWarning(fbEx, "Failed to launch fallback {Feed} for {Symbol}", fallback.GetType().Name, symbol);
            }
        }

        _logger.LogError("All fallback feeds failed for {Symbol}", symbol);
    }

    private static string MapSymbol(string symbol)
    {
        return symbol
            .Replace(".NASDAQ", ".US", StringComparison.OrdinalIgnoreCase)
            .Replace(".NYSE", ".US", StringComparison.OrdinalIgnoreCase)
            .Replace(".MTA", ".MI", StringComparison.OrdinalIgnoreCase);
    }

    private class IntradayBar
    {
        [JsonPropertyName("datetime")]
        [JsonConverter(typeof(SafeDateTimeConverter))]
        public DateTime Datetime { get; set; }

        [JsonPropertyName("open")]
        public decimal? Open { get; set; }

        [JsonPropertyName("high")]
        public decimal? High { get; set; }

        [JsonPropertyName("low")]
        public decimal? Low { get; set; }

        [JsonPropertyName("close")]
        public decimal? Close { get; set; }

        [JsonPropertyName("volume")]
        public long? Volume { get; set; }

        public bool IsValidPrice => Open.HasValue && High.HasValue && Low.HasValue && Close.HasValue;
    }
}
