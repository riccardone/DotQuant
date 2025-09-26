using DotQuant.Api.Contracts.Models.Trading;

namespace DotQuant.Api.Contracts;

public interface IDataReader
{
    bool ConfirmApiKey(string tenantId, string apiKey);
    bool TryAuthenticateUser(string userId, string password, string tenantId, out TradingDoc? tradingdoc);
    Task<Portfolio[]> GetPortfoliosAsync(string tenantId, string id);
    Task<ManagedIndividual?> GetIdentityAsync(string tenantId, string userId);
    Task<ManagedIndividualFinancials?> GetIdentityFinancialsAsync(string tenantId, string id);
}

