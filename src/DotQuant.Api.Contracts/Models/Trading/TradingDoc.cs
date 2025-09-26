namespace DotQuant.Api.Contracts.Models.Trading;

public class TradingDoc 
{
    public string Id { get; set; }
    public string CorrelationId { get; set; }
    public string DocType { get; set; } // individual, bankaccount, deposit
    public DateTime Applies { get; set; }

    // IndividualOnboardedV1
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public string PostCode { get; set; }
    public string City { get; set; }
    public string Phone { get; set; }
    public string CountryCode { get; set; }

    // BankAccountLinkedV1
    public string BankAccountLink { get; set; }
    public string Provider { get; set; }
    
    // FundsDepositedV1
    public string Currency { get; set; }
    public decimal AvailableFunds { get; set; }
}