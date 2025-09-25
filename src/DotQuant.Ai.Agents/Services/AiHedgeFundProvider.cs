using DotQuant.Core.Services.AnalysisModels;
using DotQuant.Core.Services;
using DotQuant.Ai.Contracts.Model;

namespace DotQuant.Ai.Agents.Services;

public class AiHedgeFundProvider(
    TradingInitializer initializer,
    PortfolioManager portfolioManager,
    RiskManagerAgent riskManagerAgent)
    : IAiHedgeFundProvider
{
    public async Task<TickerAnalysisResult?> PerformAnalysisAsync(string agentId, string ticker)
    {
        // Initialize workflow state for the requested ticker and agent
        var state = await initializer.InitializeAsync();
        state.SelectedAnalysts = [agentId];
        state.Tickers = [ticker];

        // Evaluate agent and run risk assessment
        portfolioManager.Evaluate(agentId, state);
        portfolioManager.RunRiskAssessments(state, riskManagerAgent);

        // Extract the agent report for the ticker
        if (!state.AnalystSignals.TryGetValue(agentId, out var tickerReports) ||
            !tickerReports.TryGetValue(ticker, out var agentReport) ||
            agentReport == null) return null;

        // Map AgentReport directly to TickerAnalysisResult
        var result = new TickerAnalysisResult
        {
            AgentName = agentReport.AgentName,
            TradeSignal = agentReport.TradeSignal,
            Confidence = agentReport.Confidence,
            Reasoning = agentReport.Reasoning,
            FinancialAnalysisResults = agentReport.FinancialAnalysisResults?.ToList() ?? new List<FinancialAnalysisResult>()
        };
        return result;
    }
}
