using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotQuant.Core.Common;
using Microsoft.Extensions.Configuration;

namespace DotQuant.Core.Services;

public class MarketStatusService : IMarketStatusService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly Dictionary<string, MarketConfig> _marketConfigs;
    private readonly Dictionary<string, (bool isOpen, DateTime dateChecked)> _cache = new();

    public MarketStatusService(HttpClient http, IConfiguration config)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Missing OpenAI:ApiKey");

        _http.BaseAddress ??= new Uri("https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        // Correctly binds the inner section only
        var marketHoursSection = config.GetSection("MarketHours");
        _marketConfigs = marketHoursSection
            .GetChildren()
            .ToDictionary(
                section => section.Key,
                section => section.Get<MarketConfig>() ?? new MarketConfig(),
                StringComparer.OrdinalIgnoreCase
            );
    }

    public async Task<bool> IsMarketOpenAsync(Symbol symbol, CancellationToken ct = default)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        var marketKey = symbol.Exchange;
        var utcNow = DateTime.UtcNow;
        var today = utcNow.Date;

        if (_cache.TryGetValue(marketKey, out var cached) && cached.dateChecked == today)
            return cached.isOpen;

        if (_marketConfigs.TryGetValue(marketKey, out var config))
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(config.Timezone);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
            var localDate = localNow.Date;

            // Weekend check
            if (localNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return CacheAndReturn(marketKey, false, today);

            // Holiday check
            if (config.Holidays.Contains(localDate.ToString("yyyy-MM-dd")))
                return CacheAndReturn(marketKey, false, today);

            // Hour check
            var open = TimeOnly.ParseExact(config.Open, "HH:mm", CultureInfo.InvariantCulture);
            var close = TimeOnly.ParseExact(config.Close, "HH:mm", CultureInfo.InvariantCulture);
            var now = TimeOnly.FromDateTime(localNow);

            var isOpen = now >= open && now <= close;
            return CacheAndReturn(marketKey, isOpen, today);
        }

        // Fallback to OpenAI if config missing
        return await QueryOpenAiFallbackAsync(marketKey, utcNow, today, ct);
    }

    private async Task<bool> QueryOpenAiFallbackAsync(string marketKey, DateTime utcNow, DateTime today, CancellationToken ct)
    {
        _marketConfigs.TryGetValue(marketKey, out var market);
        var country = market?.Country ?? "Unknown country";
        var timezoneId = market?.Timezone ?? "UTC";

        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        var localTimeString = localNow.ToString("yyyy-MM-dd 'at' HH:mm", CultureInfo.InvariantCulture);
        var prompt = $"""
                          You are a financial market assistant.

                          Is the {marketKey} stock exchange in {country} open for trading on {localTimeString} local time?

                          Answer only with "Yes" or "No".

                          Use the official trading hours of {marketKey}, and consider local public holidays and weekends.
                      """;

        var requestObj = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are a financial assistant that answers with accurate trading market status." },
                new { role = "user", content = prompt }
            },
            temperature = 0.0
        };

        var json = JsonSerializer.Serialize(requestObj);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync("chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var reply = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim()
            .ToLowerInvariant();

        var isOpen = reply?.StartsWith("yes") == true;
        return CacheAndReturn(marketKey, isOpen, today);
    }

    private bool CacheAndReturn(string key, bool isOpen, DateTime day)
    {
        _cache[key] = (isOpen, day);
        return isOpen;
    }
}
