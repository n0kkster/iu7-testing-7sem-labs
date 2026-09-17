using Analyzer.Shared.DTO;

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

}