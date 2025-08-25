namespace DotQuant.Core.Services;

public class MarketHoursRoot
{
    public Dictionary<string, MarketConfig> MarketHours { get; set; } = new();
}

public class MarketConfig
{
    public string Country { get; set; } = "";
    public string Timezone { get; set; } = "";
    public string Open { get; set; } = "";
    public string Close { get; set; } = "";
    public List<string> Holidays { get; set; } = new();
}