using DotQuant.Core.Services;
using DotQuant.Core.Services.AnalysisModels;

namespace DotQuant.Api.Code;

public class FakeAiHedgeFundProvider : IAiHedgeFundProvider
{
    public async Task<TickerAnalysisResult?> PerformAnalysisAsync(string agentId, string ticker)
    {
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("AgentId and ticker must be provided.");

        // Simulate fetching agent details and performing analysis
        var agentName = $"Agent-{agentId}"; // Example agent name generation
        var confidence = new Random().Next(50, 100) / 100m; // Simulated confidence level
        var reasoning = $"Analysis performed for ticker {ticker} by agent {agentName}.";

        // Return the analysis result
        return await Task.FromResult(new TickerAnalysisResult
        {
            AgentName = agentName, Confidence = confidence, Reasoning = reasoning
        });
    }
}