using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DotQuant.Ui.Services;

public class TestAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentUser));
    }

    public Task LoginAsync(string username)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "BlazorAuth"));

        // Notify Blazor that the authentication state has changed
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

        return Task.CompletedTask;
    }

    //public Task LogoutAsync()
    //{
    //    _currentUser = new ClaimsPrincipal(new ClaimsIdentity()); // Empty identity = logged out

    //    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

    //    return Task.CompletedTask;
    //}
}
