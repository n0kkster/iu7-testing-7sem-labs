using System.Security.Claims;
using Analyzer.Shared.DTO;

namespace Analyzer.Client.Services;

public interface IAuthService
{
    Task<ClaimsPrincipal?> LoginAsync(LoginDto dto);
    Task<bool> RegisterAsync(RegisterDto dto);
}