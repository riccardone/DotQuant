using System.Text.Json.Serialization;

namespace DotQuant.Feeds.AlphaVantage.AlphaVantage;

public class TimeSeriesDailyResponse
{
    [JsonPropertyName("Time Series (Daily)")]
    public Dictionary<string, Dictionary<string, string>> TimeSeries { get; set; } = new();
}