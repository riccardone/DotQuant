using DotQuant.Core.Services;
using DotQuant.Core.Services.GraphModels;
using Microsoft.AspNetCore.Mvc;

namespace DotQuant.Api.Controllers;

[ApiController]
[Route("session")]
public class SessionController : ControllerBase
{
    private readonly ISessionGraphProvider _provider;

    public SessionController(ISessionGraphProvider provider)
    {
        _provider = provider;
    }

    [HttpGet("graph")]
    public async Task<ActionResult<SessionGraphData>> GetSessionGraph()
    {
        var data = await _provider.GetGraphDataAsync();
        return Ok(data);
    }
}