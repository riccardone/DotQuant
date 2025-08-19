namespace DotQuant.Brokers.Trading212;

public class Trading212Position
{
    public string Instrument { get; set; }
    public string Ticker { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PnL { get; set; }
    public string Currency { get; set; }
}