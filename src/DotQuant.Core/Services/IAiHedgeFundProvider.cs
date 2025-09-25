using DotQuant.Core.Services.AnalysisModels;

namespace DotQuant.Core.Services;

public interface IAiHedgeFundProvider
{
    Task<TickerAnalysisResult?> PerformAnalysisAsync(string agentId, string ticker);
}