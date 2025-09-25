using DotQuant.Core.Services.AnalysisModels;
using System.Threading.Tasks;

namespace DotQuant.Core.Services;

public interface IAiHedgeFundProvider
{
    Task<TickerAnalysisResult?> GetGraphDataAsync(string agentId, string ticker);
}