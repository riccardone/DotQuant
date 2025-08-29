using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodHistoricalDataFeed : IFeed
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly string[] _symbols;
    private readonly string _apiKey;
    private readonly TimeSpan _interval;

    public EodHistoricalDataFeed(ILogger logger, IHttpClientFactory httpFactory, IConfiguration config, string apiKey, string[] symbols, TimeSpan? interval = null)
    {
        _logger = logger;
        _httpFactory = httpFactory;
        _config = config;
        _apiKey = apiKey;
        _symbols = symbols;
        _interval = interval ?? TimeSpan.FromSeconds(30);
    }

    public async Task PlayAsync(ChannelWriter<Event> writer, CancellationToken cancellationToken)
    {
        var client = _httpFactory.CreateClient();

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var symbol in _symbols)
            {
                try
                {
                    var adjustedSymbol = MapSymbol(symbol); // handle .MTA → .MI
                    var url = $"https://eodhistoricaldata.com/api/intraday/{adjustedSymbol}?interval=5m&range=1d&api_token={_apiKey}&fmt=json";
                    var response = await client.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogDebug("Intraday payload for {Symbol}: {Json}", symbol, json);

                    var bars = JsonSerializer.Deserialize<List<IntradayBar>>(json);
                    var latest = bars?.LastOrDefault();
                    if (latest == null)
                    {
                        _logger.LogWarning("No intraday bars returned for {Symbol}", symbol);
                        continue;
                    }

                    var parts = symbol.Split('.');
                    if (parts.Length != 2)
                        throw new FormatException($"Invalid symbol format: {symbol}. Expected format 'TICKER.EXCHANGE'");

                    var sym = new Symbol(parts[0], parts[1]);
                    var currencyCode = _config[$"MarketHours:{parts[1]}:Currency"];
                    if (string.IsNullOrWhiteSpace(currencyCode))
                        throw new InvalidOperationException($"Missing Currency entry for exchange '{parts[1]}' in MarketHours section.");

                    var currency = Currency.GetInstance(currencyCode);
                    var asset = new Stock(sym, currency);

                    var evt = new Event(DateTimeOffset.UtcNow, new List<PriceItem>
                    {
                        new PriceItem(
                            asset,
                            latest.Open,
                            latest.High,
                            latest.Low,
                            latest.Close,
                            latest.Volume,
                            _interval)
                    });

                    await writer.WriteAsync(evt, cancellationToken);
                    _logger.LogInformation("EOD intraday price for {Symbol}: {Price}", symbol, latest.Close);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch intraday price for {symbol}", symbol);
                }
            }

            await Task.Delay(_interval, cancellationToken);
        }
    }

    private static string MapSymbol(string symbol)
    {
        return symbol
            .Replace(".NASDAQ", ".US", StringComparison.OrdinalIgnoreCase)
            .Replace(".NYSE", ".US", StringComparison.OrdinalIgnoreCase)
            .Replace(".MTA", ".XETRA", StringComparison.OrdinalIgnoreCase);
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
