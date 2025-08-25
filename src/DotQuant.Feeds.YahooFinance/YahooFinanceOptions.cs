namespace DotQuant.Feeds.YahooFinance;

public class YahooFinanceOptions
{
    public int ThrottleMs { get; set; } = 500;
    public int MaxRetries { get; set; } = 3;
}