namespace DotQuant.Core.Services.AnalysisModels;

public class TickerAnalysisResult
{
    public string AgentName { get; set; } = string.Empty;
    // TODO do not depend on TradeSignal from external services
    // public TradeSignal TradeSignal { get; set; } = new();
    public decimal Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    // TODO do not depend on TradeSignal from external services
    //public List<FinancialAnalysisResult> FinancialAnalysisResults { get; set; } = new();
}