using Finbuckle.MultiTenant;

namespace DotQuant.Api.Services;

public class PreludeTenantInfo : TenantInfo
{
    public string MessageBusLink { get; set; }
    public string CryptoKey { get; set; }
    public bool IngestInvalidPayloads { get; set; }
    public string ValidationErrorField { get; set; }
    public string QueueName { get; set; }
    public string QueueNamePrefix { get; set; } 
}