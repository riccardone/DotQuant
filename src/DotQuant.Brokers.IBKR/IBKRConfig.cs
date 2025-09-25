namespace DotQuant.Brokers.IBKR;

public class IBKRConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7497;
    public string Account { get; set; } = "";
    public int Client { get; set; } = 1;
}