namespace DotQuant.Api.Contracts.Models.Trading;

public class ManagedIndividualFinancials
{
    public string Id { get; set; }
    public string BankAccountLink { get; set; }
    public string Provider { get; set; }
    public string Currency { get; set; }
    public decimal AvailableFunds { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}