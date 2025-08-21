using System.Text.Json.Serialization;

namespace DotQuant.Feeds.AlphaVantage.AlphaVantage;

public class NewsSentimentRaw
{
    [JsonPropertyName("feed")]
    public List<Dictionary<string, object>>? Feed { get; set; }
}