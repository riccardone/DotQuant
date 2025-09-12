using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using DotQuant.Core.Extensions;

namespace DotQuant.Feeds.EodHistoricalData;

public class EodRestQuoteFeed : LiveFeed
{
    private readonly ILogger<EodRestQuoteFeed> _logger;
    private readonly string _apiKey;
    private readonly string[] _symbols;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _pollInterval;
    private CancellationToken _ct;

    public EodRestQuoteFeed(string apiKey, string[] symbols, ILogger<EodRestQuoteFeed> logger, IConfiguration config, IMarketStatusService marketStatusService, TimeSpan? pollInterval = null)
    {
        _apiKey = apiKey;
        _symbols = symbols;
        _logger = logger;
        _config = config;
        _httpClient = new HttpClient();
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
        EnableMarketStatus(marketStatusService, logger);
    }

    public override async Task PlayAsync(ChannelWriter<Event> channel, CancellationToken ct)
    {
        _ct = ct;
        while (!_ct.IsCancellationRequested)
        {
            try
            {
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
                    _logger.LogInformation("All markets are closed for the requested symbols. Will retry in {Delay} seconds...", _pollInterval.TotalSeconds);
                    await Task.Delay(_pollInterval, ct);
                    continue;
                }
                var url = $"https://eodhd.com/api/real-time/{string.Join(",", openSymbols)}?api_token={_apiKey}&fmt=json";
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("EODHD REST API returned status {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    await Task.Delay(_pollInterval, ct);
                    continue;
                }
                var json = await response.Content.ReadAsStringAsync(ct);
                List<RestQuote>? quotes = null;
                try
                {
                    quotes = JsonSerializer.Deserialize<List<RestQuote>>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize EODHD REST API response: {Json}", json);
                }
                if (quotes != null)
                {
                    var now = DateTimeOffset.UtcNow;
                    var items = new List<PriceItem>();
                    foreach (var q in quotes)
                    {
                        if (string.IsNullOrEmpty(q.Code) || q.Close == null) continue;
                        var parts = q.Code.Split('.');
                        if (parts.Length != 2) continue;
                        var symbol = new Symbol(parts[0], parts[1]);
                        var currency = _config.ResolveCurrency(symbol, _logger);
                        var asset = new Stock(symbol, currency);
                        items.Add(new PriceItem(asset, q.Open ?? q.Close.Value, q.High ?? q.Close.Value, q.Low ?? q.Close.Value, q.Close.Value, q.Volume ?? 0, _pollInterval));
                    }
                    if (items.Count > 0)
                    {
                        var evt = new Event(now, items);
                        await SendAsync(evt);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EodRestQuoteFeed polling loop");
            }
            await Task.Delay(_pollInterval, ct);
        }
    }

    private class RestQuote
    {
        [JsonPropertyName("code")] public string Code { get; set; }
        [JsonPropertyName("close")] public decimal? Close { get; set; }
        [JsonPropertyName("open")] public decimal? Open { get; set; }
        [JsonPropertyName("high")] public decimal? High { get; set; }
        [JsonPropertyName("low")] public decimal? Low { get; set; }
        [JsonPropertyName("volume")] public long? Volume { get; set; }
    }
}
