using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DotQuant.Ui.Services;

public class AuthService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly SynchronizationContext? _syncContext;
    private string _userName = "Guest";
    private readonly UserContext _user = new();

    public event Action? OnUserChanged;

    public AuthService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _syncContext = SynchronizationContext.Current; // Capture UI context

        _authenticationStateProvider.AuthenticationStateChanged += async task =>
        {
            await UpdateUserContextAsync(task.Result);
        };
    }

    public string UserName => _userName;
    public UserContext User => _user;

    public async Task InitializeAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        await UpdateUserContextAsync(authState);
    }

    private async Task UpdateUserContextAsync(AuthenticationState authState)
    {
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _userName = user.FindFirst(ClaimTypes.Name).Value;

            _user.TenantId = user.FindFirst("tenantId")?.Value ?? "default";
            _user.FirstName = user.FindFirst(ClaimTypes.Name)?.Value;
            _user.LastName = user.FindFirst(ClaimTypes.Surname)?.Value;
            _user.Email = user.FindFirst(ClaimTypes.Email)?.Value;
            _user.Address = user.FindFirst(ClaimTypes.StreetAddress)?.Value;
            _user.PostCode = user.FindFirst(ClaimTypes.PostalCode)?.Value;
            _user.City = user.FindFirst(ClaimTypes.Locality)?.Value;
            _user.Phone = user.FindFirst(ClaimTypes.HomePhone)?.Value ?? user.FindFirst(ClaimTypes.MobilePhone)?.Value;
            _user.CountryCode = user.FindFirst(ClaimTypes.Country)?.Value;
            _user.Role = user.FindFirst(ClaimTypes.Role)?.Value ;

            // Simulated API Call: Replace this with actual data retrieval
            _user.Financials.AvailableFunds = await GetAccountBalanceAsync(_user.TenantId);
        }
        else
        {
            _userName = "Guest";
            _user.TenantId = string.Empty;
            _user.FirstName = string.Empty;
            _user.LastName = string.Empty;
            _user.Role = string.Empty;
            _user.Financials = new Financials { AvailableFunds = 0m };
        }

        // Ensure the UI updates on the main thread
        if (_syncContext != null)
        {
            _syncContext.Post(_ => OnUserChanged?.Invoke(), null);
        }
        else
        {
            OnUserChanged?.Invoke();
        }
    }

    private Task<decimal> GetAccountBalanceAsync(string tenantId)
    {
        // Simulate fetching balance from an API
        return Task.FromResult(5000.00m);
    }
}
