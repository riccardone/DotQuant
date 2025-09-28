using DotQuant.Core.Services;
using DotQuant.Core.Services.AnalysisModels;
using Microsoft.AspNetCore.Mvc;

namespace DotQuant.Api.Controllers;

[ApiController]
[Route("aihedgefund")]
public class AiHedgeFundController : Controller
{
    private readonly IAiHedgeFundProvider _provider;

    public AiHedgeFundController(IAiHedgeFundProvider provider)
    {
        _provider = provider;
    }

    [HttpGet("agents/{agentId}/tickers/{ticker}")]
    public async Task<ActionResult<TickerAnalysisResult>> PerformTickersAnalysis(string agentId, string ticker)
    {
        if (string.IsNullOrWhiteSpace(agentId) || string.IsNullOrWhiteSpace(ticker))
            return BadRequest("AgentId and ticker are required.");

        var result = await _provider.PerformAnalysisAsync(agentId, ticker);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}