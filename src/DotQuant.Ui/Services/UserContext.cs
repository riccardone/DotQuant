namespace DotQuant.Ui.Services;

public class UserContext
{
    public string Id { get; set; }
    public string Role { get; set; }
    public string TenantId { get; set; }
    public string Email { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public string PostCode { get; set; }
    public string City { get; set; }
    public string Phone { get; set; }
    public string CountryCode { get; set; }
    public Financials Financials { get; set; }
    public Portfolio[] Portfolios { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class Financials
{
    public string Id { get; set; }
    public string BankAccountLink { get; set; }
    public string Provider { get; set; }
    public string Currency { get; set; }
    public decimal AvailableFunds { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class Portfolio
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Evaluation { get; set; }
    public decimal Trend { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}