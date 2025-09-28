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
    private readonly IDataReader _dataReader;

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
        WarrenBuffettAgent warrenBuffett,
        IDataReader dataReader)
    {
        _logger = logger;
        _portfolio = portfolio;
        _riskAgent = riskAgent;
        _dataReader = dataReader;

        // Register agents in the registry (only once)
        registry.Register($"{nameof(BenGrahamAgent).ToSnakeCase()}", benGraham.Run);
        registry.Register($"{nameof(CathieWoodAgent).ToSnakeCase()}", cathieWood.Run);
        registry.Register($"{nameof(BillAckmanAgent).ToSnakeCase()}", billAckman.Run);
        registry.Register($"{nameof(CharlieMungerAgent).ToSnakeCase()}", charlieMunger.Run);
        registry.Register($"{nameof(StanleyDruckenmillerAgent).ToSnakeCase()}", stanleyDruckenmiller.Run);
        registry.Register($"{nameof(WarrenBuffettAgent).ToSnakeCase()}", warrenBuffett.Run);
    }

    public async Task<TickerAnalysisResult?> PerformAnalysisAsync(string agentId, string pTicker)
    {
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(pTicker))
        {
            _logger.LogWarning("AgentId or ticker is missing.");
            return null;
        }

        _logger.LogInformation("Initializing trading state for analysis...");
        var state = new TradingWorkflowState
        {
            Tickers = [pTicker],
            SelectedAnalysts = [agentId],
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
            RiskLevel = RiskLevel.Medium,
        };

        foreach (var ticker in state.Tickers)
        {
            if (!_dataReader.TryGetFinancialMetrics(ticker, DateTime.Today, "ttm", 10, out var metrics))
            {
                _logger.LogError($"I can't retrieve metrics for {ticker}");
                continue;
            }
            state.FinancialMetrics.Add(ticker, metrics);
            if (!_dataReader.TryGetFinancialLineItems(ticker, DateTime.Today, "ttm", 10, out var financialLineItems))
            {
                _logger.LogError($"I can't retrieve financial data for {ticker}");
                continue;
            }
            state.FinancialLineItems.Add(ticker, financialLineItems);
            if (!_dataReader.TryGetPrices(ticker, state.StartDate, state.EndDate, out var prices))
            {
                _logger.LogError($"I can't retrieve prices for {ticker}");
                continue;
            }
            if (!_dataReader.TryGetCompanyNews(ticker, out var companyNews))
            {
                _logger.LogError($"I can't retrieve company news data for {ticker}");
                continue;
            }
            state.CompanyNews.Add(ticker, companyNews);
            state.Prices.Add(ticker, prices);
        }

        await Task.CompletedTask;

        // Evaluate all selected analysts
        foreach (var agent in state.SelectedAnalysts)
        {
            _portfolio.Evaluate(agent, state);
        }

        // Run risk assessments
        _portfolio.RunRiskAssessments(state, _riskAgent);

        // Find the analysis result for the requested agent and ticker
        if (state.AnalystSignals.TryGetValue(agentId, out var tickerSignals) &&
            tickerSignals.TryGetValue(pTicker, out var signal))
        {
            // Map the signal to TickerAnalysisResult as needed
            var result = new TickerAnalysisResult
            {
                AgentName = agentId,
                Confidence = signal.Confidence, // Adjust property names as needed
                Reasoning = signal.Reasoning    // Adjust property names as needed
            };
            _logger.LogInformation("Analysis found for AgentId: {AgentId}, Ticker: {Ticker}", agentId, pTicker);
            return result;
        }

        _logger.LogWarning("No analysis result found for AgentId: {AgentId}, Ticker: {Ticker}", agentId, pTicker);
        return null;
    }
}