using DotQuant.Core.Services;
using DotQuant.Core.Services.GraphModels;
using Microsoft.AspNetCore.Mvc;

namespace DotQuant.Controllers;

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
    public ActionResult<SessionGraphData> GetSessionGraph()
    {
        return Ok(_provider.GetGraphDataAsync().GetAwaiter().GetResult());
    }
}