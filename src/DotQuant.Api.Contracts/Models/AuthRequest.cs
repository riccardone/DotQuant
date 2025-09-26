namespace DotQuant.Api.Contracts.Models;

public class AuthRequest
{
    public string UserId { get; set; }
    public string Password { get; set; }
    public string TenantId { get; set; }
}