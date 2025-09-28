using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using AiHedgeFund.Contracts.Model;
using DotQuant.Core.Services;
using DotQuant.Core.Services.AnalysisModels;

namespace DotQuant.Api.Code;

public class AiHedgeFundProvider : IAiHedgeFundProvider
{
    private readonly ILogger<AiHedgeFundProvider> _logger;
    private readonly PortfolioManager _portfolio;
    private readonly RiskManagerAgent _riskAgent;
    private readonly IAgentRegistry _registry;

    public AiHedgeFundProvider(
        ILogger<AiHedgeFundProvider> logger,
        PortfolioManager portfolio,
        RiskManagerAgent riskAgent,
        IAgentRegistry registry,
        BenGrahamAgent benGraham,
        CathieWoodAgent cathieWood,
        BillAckmanAgent billAckman,
        CharlieMungerAgent charlieMunger,
        StanleyDruckenmillerAgent stanleyDruckenmiller,
        WarrenBuffettAgent warrenBuffett)
    {
        _logger = logger;
        _portfolio = portfolio;
        _riskAgent = riskAgent;
        _registry = registry;

        // Register agents in the registry (only once)
        _registry.Register($"{nameof(BenGrahamAgent).ToSnakeCase()}", benGraham.Run);
        _registry.Register($"{nameof(CathieWoodAgent).ToSnakeCase()}", cathieWood.Run);
        _registry.Register($"{nameof(BillAckmanAgent).ToSnakeCase()}", billAckman.Run);
        _registry.Register($"{nameof(CharlieMungerAgent).ToSnakeCase()}", charlieMunger.Run);
        _registry.Register($"{nameof(StanleyDruckenmillerAgent).ToSnakeCase()}", stanleyDruckenmiller.Run);
        _registry.Register($"{nameof(WarrenBuffettAgent).ToSnakeCase()}", warrenBuffett.Run);
    }

    public async Task<TickerAnalysisResult?> PerformAnalysisAsync(string agentId, string ticker)
    {
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(ticker))
        {
            _logger.LogWarning("AgentId or ticker is missing.");
            return null;
        }

        _logger.LogInformation("Initializing trading state for analysis...");
        var state = new TradingWorkflowState
        {
            Tickers = [ticker],
            SelectedAnalysts = [agentId],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            RiskLevel = RiskLevel.Medium,
        };

        // Evaluate all selected analysts
        foreach (var agent in state.SelectedAnalysts)
        {
            _portfolio.Evaluate(agent, state);
        }

        // Run risk assessments
        _portfolio.RunRiskAssessments(state, _riskAgent);

        // Find the analysis result for the requested agent and ticker
        if (state.AnalystSignals.TryGetValue(agentId, out var tickerSignals) &&
            tickerSignals.TryGetValue(ticker, out var signal))
        {
            // Map the signal to TickerAnalysisResult as needed
            var result = new TickerAnalysisResult
            {
                AgentName = agentId,
                Confidence = signal.Confidence, // Adjust property names as needed
                Reasoning = signal.Reasoning    // Adjust property names as needed
            };
            _logger.LogInformation("Analysis found for AgentId: {AgentId}, Ticker: {Ticker}", agentId, ticker);
            return result;
        }

        _logger.LogWarning("No analysis result found for AgentId: {AgentId}, Ticker: {Ticker}", agentId, ticker);
        return null;
    }
}