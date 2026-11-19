using Analyzer.Shared.DTO.Common;
using Analyzer.Shared.DTO.V2;

namespace Analyzer.Application.Interfaces.Services;

public interface IUserService
{
    Task<Guid> RegisterAsync(RegisterDto dto);
    Task<string> LoginAsync(LoginDto dto);
    
    Task<UserDto> GetProfileAsync(Guid userId);
    Task<IReadOnlyCollection<UserDto>> GetAllUsersAsync();
    Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task DeleteAsync(Guid userId);

    Task<UserDto> PatchUserAsync(Guid userId, PatchUserDto dto);

    Task<LoginResultDto> AuthenticateAsync(LoginDto dto);
    Task<string> VerifyTwoFactorAsync(VerifyTwoFactorDto dto);
    Task RequestUnlockAsync(RequestUnlockDto dto);
    Task ConfirmUnlockAsync(ConfirmUnlockDto dto);
    Task SetTwoFactorAsync(Guid userId, bool enabled);
}