using System.Text.Json.Serialization;

namespace DotQuant.Brokers.Trading212;

public class Trading212Account
{
    [JsonPropertyName("free")]
    public decimal FreeFunds { get; set; }

    [JsonPropertyName("total")]
    public decimal Balance { get; set; }

    [JsonPropertyName("invested")]
    public decimal Invested { get; set; }

    [JsonPropertyName("result")]
    public decimal PnL { get; set; }

    [JsonPropertyName("ppl")]
    public decimal Equity { get; set; } // Adjust this if "ppl" means something else

    [JsonPropertyName("pieCash")]
    public decimal PieCash { get; set; }

    [JsonPropertyName("blocked")]
    public decimal? Blocked { get; set; }

    // Optional: derive or hardcode currency if needed
    public string Currency { get; set; } = "USD";
}