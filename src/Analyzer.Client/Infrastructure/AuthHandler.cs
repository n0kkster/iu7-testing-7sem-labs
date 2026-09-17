using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace Analyzer.Client.Infrastructure;

public class JwtAuthorizationHandler(AuthenticationStateProvider authStateProvider) : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authStateProvider = authStateProvider;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath.ToLower();
        
        if (path != null && (path.Contains("/login") || path.Contains("/register")))
            return await base.SendAsync(request, cancellationToken);

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        
        var token = authState.User.FindFirst("jwt-api-token")?.Value;

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}