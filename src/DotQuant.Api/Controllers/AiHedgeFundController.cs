using DotQuant.Core.Services;
using DotQuant.Core.Services.AnalysisModels;
using Microsoft.AspNetCore.Mvc;

namespace DotQuant.Api.Controllers;

[ApiController]
[Route("aihedgefund")]
public class AiHedgeFundController : Controller
{
    private readonly IAiHedgeFundProvider _provider;
    private readonly ILogger<AiHedgeFundController> _logger;

    public AiHedgeFundController(IAiHedgeFundProvider provider, ILogger<AiHedgeFundController> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    [HttpGet("agents/{agentId}/tickers/{ticker}")]
    public async Task<ActionResult<TickerAnalysisResult>> PerformTickersAnalysis(string agentId, string ticker)
    {
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(ticker))
        {
            _logger.LogError("AgentId or ticker is missing.");
            return BadRequest("AgentId and ticker are required.");
        }

        _logger.LogInformation("Performing analysis for AgentId: {AgentId}, Ticker: {Ticker}", agentId, ticker);
        var result = await _provider.PerformAnalysisAsync(agentId, ticker);
        if (result == null)
        {
            _logger.LogInformation("No analysis result found for AgentId: {AgentId}, Ticker: {Ticker}", agentId, ticker);
            return NotFound();
        }

        _logger.LogInformation("Analysis completed for AgentId: {AgentId}, Ticker: {Ticker}", agentId, ticker);
        return Ok(result);
    }
}