using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotQuant.Feeds.AlphaVantage.AlphaVantage;

public class DataFetcher
{
    private readonly ILogger<DataFetcher> _logger;
    private readonly FileDataManager _dataManager;
    private readonly ConcurrentDictionary<string, object?> _memoryCache = new();
    private readonly HttpClient _client;

    public DataFetcher(IHttpClientFactory httpClientFactory, ILogger<DataFetcher> logger)
    {
        _logger = logger;
        _dataManager = new FileDataManager();
        _client = httpClientFactory.CreateClient("AlphaVantage");
    }

    public bool TryLoadOrFetch<TRaw, T>(string key, string query, Func<TRaw, T> mapper, out T? result)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
        {
            result = typed;
            return true;
        }

        var raw = _dataManager.Read<TRaw>(key);

        if (raw == null && TryFetchData(query, out raw))
        {
            _dataManager.Save(raw, key);
        }

        if (raw == null)
        {
            result = default;
            return false;
        }

        try
        {
            var mapped = mapper(raw);
            _memoryCache[key] = mapped!;
            result = mapped;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map data for key '{Key}'", key);
            result = default;
            return false;
        }
    }

    public bool TryLoadOrFetchRawJson<T>(string key, string query, Func<string, T?> parser, out T? result)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T typed)
        {
            result = typed;
            return true;
        }

        var rawJson = _dataManager.ReadRawJson(key);

        if (rawJson == null && TryFetchRawJson(query, out rawJson))
        {
            _dataManager.SaveRawJson(rawJson, key);
        }

        if (rawJson == null)
        {
            result = default;
            return false;
        }

        try
        {
            var parsed = parser(rawJson);
            if (parsed == null)
            {
                _logger.LogWarning("Parsed result is null for key '{Key}'", key);
                result = default;
                return false;
            }

            _memoryCache[key] = parsed;
            result = parsed;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse raw JSON for key '{Key}'", key);
            result = default;
            return false;
        }
    }

    private bool TryFetchData<T>(string endpoint, out T? result)
    {
        result = default;

        try
        {
            var response = _client.GetAsync(endpoint).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API call to '{Endpoint}' failed with status code {StatusCode}", endpoint, response.StatusCode);
                return false;
            }

            var jsonString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (jsonString.Contains("detected your API key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("API key error in response from '{Endpoint}': {Message}", endpoint, jsonString);
                return false;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            result = JsonSerializer.Deserialize<T>(jsonString, options);

            if (result == null)
            {
                _logger.LogWarning("Deserialization returned null for endpoint '{Endpoint}'", endpoint);
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: {Message} '{Endpoint}'", ex.GetBaseException().Message, endpoint);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed: {Message} '{Endpoint}'", ex.GetBaseException().Message, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error: {Message} '{Endpoint}'", ex.GetBaseException().Message, endpoint);
        }

        return false;
    }

    private bool TryFetchRawJson(string endpoint, out string? json)
    {
        json = null;

        try
        {
            var response = _client.GetAsync(endpoint).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API call to '{Endpoint}' failed with status code {StatusCode}", endpoint, response.StatusCode);
                return false;
            }

            json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (json.Contains("detected your API key", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("API key error in response from '{Endpoint}': {Message}", endpoint, json);
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: {Message} '{Endpoint}'", ex.GetBaseException().Message, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching raw JSON: {Message} '{Endpoint}'", ex.GetBaseException().Message, endpoint);
        }

        return false;
    }
}
