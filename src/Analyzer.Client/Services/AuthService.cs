using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Analyzer.Shared.DTO;
using Analyzer.Client.Utils;
using Serilog;

namespace Analyzer.Client.Services;

public class AuthService(HttpClient httpClient) : IAuthService
{
    public async Task<ClaimsPrincipal?> LoginAsync(LoginDto dto)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/users/login", dto);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var authResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (string.IsNullOrEmpty(authResponse?.Token))
            {
                Log.Error("Ошибка получения токена при логине.");
                return null;
            }
            
            // Парсим клаймы из токена и добавляем сам токен как клайм
            var claims = JwtHelper.ParseClaimsFromJwt(authResponse.Token).ToList();            
            claims.Add(new Claim("jwt-api-token", authResponse.Token));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка авторизации.");
            return null;
        }
    }

    public async Task<bool> RegisterAsync(RegisterDto dto)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/users/register", dto);
            
            if (!response.IsSuccessStatusCode)
                return false;

            var newUserId = await response.Content.ReadFromJsonAsync<Guid>();
            if (newUserId == Guid.Empty)
            {
                Log.Error("Ошибка получения id при регистрации.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка регистрации.");
            return false;
        }
    }
}

public record LoginResponse(string Token);