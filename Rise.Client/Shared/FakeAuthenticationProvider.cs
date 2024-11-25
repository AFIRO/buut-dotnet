using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Rise.Client.Auth;

public class FakeAuthenticationProvider: AuthenticationStateProvider
{
    private readonly string userId;
    private readonly string[] roles;
    
    public FakeAuthenticationProvider(string userId, params string[] roles)
    {
        this.userId = userId;
        this.roles = roles;
    }
    
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        
        // Create claims for the user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId) // Auth0-style subject claim
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Create identity and principal
        var identity = new ClaimsIdentity(claims, "Fake authentication");
        var user = new ClaimsPrincipal(identity);

        return Task.FromResult(new AuthenticationState(user));    }
}