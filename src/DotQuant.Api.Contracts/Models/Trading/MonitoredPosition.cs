namespace DotQuant.Api.Contracts.Models.Trading;

public class MonitoredPosition
{
    public string Id { get; set; }
    public string CorrelationId { get; set; }
    public string PositionType { get; set; }
    public string Ticker { get; set; }
    public string Exchange { get; set; }
}