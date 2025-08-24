using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotQuant.Core.Common;
using Microsoft.Extensions.Configuration;

namespace DotQuant.Core.Services;

public class OpenAiMarketStatusService : IMarketStatusService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly Dictionary<string, (bool isOpen, DateTime dateChecked)> _cache = new();

    public OpenAiMarketStatusService(HttpClient http, IConfiguration config)
    {
        _apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Missing OpenAI:ApiKey in configuration");

        _http = http ?? throw new ArgumentNullException(nameof(http));
        _http.BaseAddress ??= new Uri("https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<bool> IsMarketOpenAsync(Symbol symbol, CancellationToken ct = default)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        var today = DateTime.UtcNow.Date;

        // Quick weekend check
        if (today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday)
            return false;

        var marketKey = symbol.Exchange;

        if (_cache.TryGetValue(marketKey, out var cached) && cached.dateChecked == today)
            return cached.isOpen;

        // Call to GPT (less trusted)
        var prompt = $"Is the {marketKey} stock market open for trading today? Today is {today:MMMM dd, yyyy}. Answer only with Yes or No.";

        var request = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "You are a financial assistant that answers with accurate trading market status." },
                new { role = "user", content = prompt }
            },
            temperature = 0.0
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync("chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: ct);

        var reply = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim()
            .ToLowerInvariant();

        var isOpen = reply?.StartsWith("yes") == true;

        _cache[marketKey] = (isOpen, today);
        return isOpen;
    }
}
