using DotQuant.Api.Models;

namespace DotQuant.Api.Services;

public interface ISessionGraphProvider
{
    Task<SessionGraphData> GetGraphDataAsync();
}