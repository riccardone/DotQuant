using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace DotQuant.Ui.Services;

public class AuthRequest
{
    public AuthRequest(string userId, string password, string tenantId)
    {
        UserId = userId;
        Password = password;
        TenantId = tenantId;
    }

    public string UserId { get; }
    public string Password { get; }
    public string TenantId { get; }
}

public class ApiAuthStateProvider : AuthenticationStateProvider
{
    private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly IHttpClientFactory _httpClientFactory;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public ApiAuthStateProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        // Temporary workaround to avoid wasting time on auth
        NotifyAuthStateChanged(new AuthResponse
        {
            AccessToken = Guid.NewGuid().ToString(),
            GivenName = "Demo",
            Surname = "User",
            Email = "test@test.com",
            Address = "123 Demo St",
            PostCode = "12345",
            City = "Demo City",
            Phone = "123-456-7890",
            CountryCode = "UK",
            Role = "Admin"
        });
        return true;

        var httpClient = _httpClientFactory.CreateClient("PreludeApi");

        var response = await httpClient.PostAsJsonAsync("Auth/login", new AuthRequest(username.CreateCorrelationId(), password, "default"));

        if (!response.IsSuccessStatusCode)
        {
            _logger.Error($"Login failed: {response.StatusCode}, Response: {response.ReasonPhrase}");
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            _logger.Error("Login failed: Response content is empty.");
            return false;
        }

        try
        {
            var authResult = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!NotifyAuthStateChanged(authResult)) return false;

            return true;
        }
        catch (JsonException ex)
        {
            _logger.Error($"JSON Deserialization error: {ex.Message}");
            return false;
        }
    }

    private bool NotifyAuthStateChanged(AuthResponse? authResult)
    {
        if (authResult == null)
        {
            _logger.Error("Login failed: Deserialization returned null.");
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, authResult.GivenName),
            new(ClaimTypes.Surname, authResult.Surname),
            new(ClaimTypes.Role, authResult.Role),
            new(ClaimTypes.Email, authResult.Email),
            new(ClaimTypes.StreetAddress, authResult.Address),
            new(ClaimTypes.PostalCode, authResult.PostCode),
            new(ClaimTypes.Locality, authResult.City),
            new(ClaimTypes.Country, authResult.CountryCode),
            new("AccessToken", authResult.AccessToken)
        };

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiAuth"));

        // TODO retrieve the currentuser financials

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        return true;
    }


    public Task LogoutAsync()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        return Task.CompletedTask;
    }
}

public class AuthResponse
{
    public string AccessToken { get; set; }
    public string GivenName { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string PostCode { get; set; }
    public string City { get; set; }
    public string Phone { get; set; }
    public string CountryCode { get; set; }
    public string Role { get; set; }
}