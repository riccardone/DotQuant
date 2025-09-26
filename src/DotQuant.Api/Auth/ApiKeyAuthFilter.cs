using DotQuant.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace DotQuant.Api.Auth;

public class ApiKeyAuthFilter : IAsyncAuthorizationFilter
{
    private readonly IConfiguration _configuration;
    private readonly IBaasAuthoriser _authoriser;
    private readonly ILogger<ApiKeyAuthFilter> _logger; 

    public ApiKeyAuthFilter(IConfiguration configuration, IBaasAuthoriser auth, ILogger<ApiKeyAuthFilter> logger)
    {
        _configuration = configuration;
        _authoriser = auth;
        _logger = logger; 
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
#if !DEBUG
        if (!context.HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("Api key missing");
            return;
        }
#else
        var extractedApiKey = "DEBUG";
#endif

        _logger.LogDebug($"Extracted Api Key: {extractedApiKey}");
        _logger.LogDebug($"Extracted Api Key ToString: {extractedApiKey.ToString()}");

        if(context.HttpContext.Request.Method != "POST")
        {
            if (!context.HttpContext.Request.RouteValues.TryGetValue("tenantId", out var tenantId))
            {
                context.Result = new BadRequestObjectResult("No TenantId found in Route Parameters");
            }
        }        

        //bool apiKeyValid = _authoriser.CheckApiKey(tenantId.ToString(), extractedApiKey);
        //if (!apiKeyValid)
        //{
        //    context.Result = new UnauthorizedObjectResult("Invalid Api Key");
        //}
    }
}