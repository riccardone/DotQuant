using DotQuant.Ai.Contracts;
using DotQuant.Ai.Contracts.Model;

namespace DotQuant.Core.Services.AnalysisModels;

public class TickerAnalysisResult
{
    public string AgentName { get; set; } = string.Empty;
    public TradeSignal TradeSignal { get; set; } = new();
    public decimal Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<FinancialAnalysisResult> FinancialAnalysisResults { get; set; } = new();
}