using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Analyzer.Client.Providers;

public class AuthStateProvider(IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        
        return Task.FromResult(new AuthenticationState(user));
    }
}