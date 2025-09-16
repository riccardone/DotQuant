using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace DotQuant.Core.Services;

public class RateLimitedHttpClient
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly Dictionary<string, DateTimeOffset> _lastCall = new();

    public RateLimitedHttpClient(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<T?> GetJsonWithBackoff<T>(string url, string key, int minDelaySeconds, CancellationToken ct = default)
    {
        if (_lastCall.TryGetValue(key, out var last) &&
            DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(minDelaySeconds))
        {
            var wait = TimeSpan.FromSeconds(minDelaySeconds) - (DateTimeOffset.UtcNow - last);
            _logger.LogDebug("Rate limit triggered for {Key}, waiting {Wait}", key, wait);
            await Task.Delay(wait, ct);
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            var response = await _client.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                _lastCall[key] = DateTimeOffset.UtcNow;
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            }

            if ((int)response.StatusCode == 429)
            {
                _logger.LogWarning("429 Too Many Requests on {Key}. Retrying...", key);
                await Task.Delay(1000 * (attempt + 1), ct);
                continue;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Request to {Url} returned 404 Not Found (expected in demo mode)", url);
            }
            else
            {
                _logger.LogError("Request to {Url} failed: {Code}", url, response.StatusCode);
            }

            break;
        }

        return default;
    }
}