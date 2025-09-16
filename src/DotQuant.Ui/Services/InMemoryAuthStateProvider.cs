using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DotQuant.Ui.Services;

public class InMemoryAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity()); // Initially no user

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public Task LoginAsync(string username)
    {
        // Create user claims (Minimal Role-Based Access)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "User") // Can be "Admin" or "User"
        };

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "InMemoryAuth"));

        // Notify the system that the authentication state has changed
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

        return Task.CompletedTask;
    }

    public Task LogoutAsync()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity()); // Clear user identity

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

        return Task.CompletedTask;
    }
}