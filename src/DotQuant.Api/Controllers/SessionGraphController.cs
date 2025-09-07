using System.Text.Json;
using System.Text.Json.Serialization;
using DotQuant.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotQuant.Api.Controllers;

/// <summary>
/// Controller for session time-series data (price, signals, orders)
/// </summary>
[ApiController]
[Route("session")]
public class SessionGraphController : ControllerBase
{
    private readonly ISessionGraphProvider _provider;
    private readonly ILogger<SessionGraphController> _logger;

    public SessionGraphController(ISessionGraphProvider provider, ILogger<SessionGraphController> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    /// <summary>
    /// Returns session graph data for plotting
    /// </summary>
    [HttpGet("graph")]
    public async Task<IActionResult> GetSessionGraphData()
    {
        var data = await _provider.GetGraphDataAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return Ok(data);
    }
}