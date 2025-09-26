namespace DotQuant.Api.Contracts.Models.Trading;

public class Portfolio
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Evaluation { get; set; }
    public decimal Trend { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}