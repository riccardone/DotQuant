using DotQuant.Api.Contracts;
using DotQuant.Api.Contracts.Models.Trading;

namespace DotQuant.Api.Services
{
    public class FakeDataReader : IDataReader
    {
        public bool ConfirmApiKey(string tenantId, string apiKey)
        {
            return true;
        }

        public bool TryAuthenticateUser(string userId, string password, string tenantId, out TradingDoc? tradingdoc)
        {
            tradingdoc = new TradingDoc
            {
                Id = "test-user",
                Email = "ciao@ciao.com"
            };
            return true;
        }

        public Task<Portfolio[]> GetPortfoliosAsync(string tenantId, string id)
        {
            return Task.FromResult(new[]
            {
                new Portfolio
                {
                    Id = "portfolio-1",
                    Name = "My Portfolio",
                    Evaluation = 10000,
                    Trend = 5.5m,
                    LastUpdatedAt = DateTime.UtcNow
                },
                new Portfolio
                {
                    Id = "portfolio-2",
                    Name = "Retirement Fund",
                    Evaluation = 25000,
                    Trend = 3.2m,
                    LastUpdatedAt = DateTime.UtcNow
                }
            });
        }

        public Task<ManagedIndividual?> GetIdentityAsync(string tenantId, string userId)
        {
            return Task.FromResult<ManagedIndividual?>(new ManagedIndividual
            {
                Id = "test-user",
                Email = "ciao@ciao.com"
            });
        }

        public Task<ManagedIndividualFinancials?> GetIdentityFinancialsAsync(string tenantId, string id)
        {
            return Task.FromResult<ManagedIndividualFinancials?>(new ManagedIndividualFinancials
            {
                Id = "test-user",
                AvailableFunds = 1000,
                BankAccountLink = "bank-link",
                Currency = "USD",
                LastUpdatedAt = DateTime.UtcNow,
                Provider = "FakeProvider"
            });
        }
    }
}
