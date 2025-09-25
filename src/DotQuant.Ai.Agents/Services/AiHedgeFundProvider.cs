using DotQuant.Core.Services.AnalysisModels;
using DotQuant.Core.Services;
using DotQuant.Ai.Contracts.Model;
using System.Threading.Tasks;

namespace DotQuant.Ai.Agents.Services;

public class AiHedgeFundProvider : IAiHedgeFundProvider
{
    private readonly TradingInitializer _initializer;
    private readonly PortfolioManager _portfolioManager;
    private readonly RiskManagerAgent _riskManagerAgent;

    public AiHedgeFundProvider(
        TradingInitializer initializer,
        PortfolioManager portfolioManager,
        RiskManagerAgent riskManagerAgent)
    {
        _initializer = initializer;
        _portfolioManager = portfolioManager;
        _riskManagerAgent = riskManagerAgent;
    }

    public async Task<TickerAnalysisResult?> GetGraphDataAsync(string agentId, string ticker)
    {
        // Initialize workflow state for the requested ticker and agent
        var state = await _initializer.InitializeAsync();
        state.SelectedAnalysts = new List<string> { agentId };
        state.Tickers = new List<string> { ticker };

        // Evaluate agent and run risk assessment
        _portfolioManager.Evaluate(agentId, state);
        _portfolioManager.RunRiskAssessments(state, _riskManagerAgent);

        // Extract the agent report for the ticker
        if (state.AnalystSignals.TryGetValue(agentId, out var tickerReports) &&
            tickerReports.TryGetValue(ticker, out var agentReport) &&
            agentReport != null)
        {
            // Map AgentReport to TickerAnalysisResult (expand as needed)
            var result = new TickerAnalysisResult
            {
                // TODO: Map fields from agentReport and state as needed
            };
            return result;
        }

        return null;
    }
}
