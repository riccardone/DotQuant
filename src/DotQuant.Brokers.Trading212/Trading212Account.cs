namespace DotQuant.Brokers.Trading212;

public class Trading212Account
{
    public string Currency { get; set; }
    public decimal Balance { get; set; }
    public decimal Invested { get; set; }
    public decimal PnL { get; set; }
    public decimal Equity { get; set; }
    public decimal FreeFunds { get; set; }
    public decimal Margin { get; set; }
}