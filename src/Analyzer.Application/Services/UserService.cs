using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO.Common;
using Analyzer.Application.Interfaces.Providers;
using Analyzer.Shared.DTO.V2;
using Analyzer.Domain.Exceptions;

namespace Analyzer.Application.Services;

public class UserService(
    IUserRepository userRepository,
    IJwtProvider jwtProvider,
    IInviteService inviteService,
    IAvatarRepository avatarRepository,
    IEmailProvider? emailProvider = null) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtProvider _jwtProvider = jwtProvider;
    private readonly IInviteService _inviteService = inviteService;
    private readonly IAvatarRepository _avatarRepository = avatarRepository;
    private readonly IEmailProvider? _emailProvider = emailProvider;

    public async Task<Guid> RegisterAsync(RegisterDto dto)
    {
        if (await _userRepository.ExistsByUsernameAsync(dto.Username))
            throw new InvalidOperationException("Пользователь с таким именем уже зарегистрирован.");

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            throw new ArgumentException("Пароль должен содержать не менее 8 символов");

        var (Role, TeamId) = await _inviteService.GetValidatedInviteDetailsAsync(dto.InviteCode, dto.Email);

        string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.Password);
        var user = User.CreateInvitedUser(
            dto.Username,
            dto.Email,
            passwordHash,
            Role,
            TeamId
        );

        await _userRepository.AddAsync(user);
        await _inviteService.ConsumeInviteAsync(dto.InviteCode, user);

        return user.Id;
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(dto.Password, user.PasswordHash))
            throw new InvalidOperationException("Неверный логин или пароль");

        string role = user.Role.ToString();

        return _jwtProvider.GenerateToken(user, role);
    }

    public async Task<UserDto> GetProfileAsync(Guid userId)
    {
        var user = await GetUserOrThrowAsync(userId);
        return MapToDto(user);
    }

    public async Task<IReadOnlyCollection<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!string.Equals(user.Username, dto.Username, StringComparison.OrdinalIgnoreCase))
            if (await _userRepository.ExistsByUsernameAsync(dto.Username))
                throw new InvalidOperationException("Это имя пользователя уже используется другим пользователем.");

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                throw new InvalidOperationException("Данная почта уже используется другим пользователем.");

        if (dto.AvatarId is not null)
        {
            var avatar = await _avatarRepository.GetByIdAsync((Guid)dto.AvatarId);
            if (avatar is null || avatar.UserId != userId)
                throw new KeyNotFoundException("Данный аватар не найден.");
        }
        
        user.UpdateProfile(dto.Username, dto.Email, dto.AvatarId);

        await _userRepository.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (!BCrypt.Net.BCrypt.EnhancedVerify(oldPassword, user.PasswordHash))
            throw new InvalidOperationException("Текущий пароль указан неверно.");
        
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            throw new ArgumentException("Пароль должен содержать не менее 8 символов.");

        string newHash = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword);

        user.ChangePassword(newHash);

        await _userRepository.UpdateAsync(user);
    }

    private async Task<User> GetUserOrThrowAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
                 ?? throw new KeyNotFoundException($"Пользователь с ID {userId} не найден.");

        return user;
    }

    public async Task DeleteAsync(Guid userId)
    {
        await GetUserOrThrowAsync(userId);
        await _userRepository.DeleteAsync(userId);
    }

    public async Task<UserDto> PatchUserAsync(Guid userId, PatchUserDto dto)
    {
        var user = await GetUserOrThrowAsync(userId);

        var newUsername = string.IsNullOrWhiteSpace(dto.Username) ? user.Username : dto.Username;
        var newEmail = string.IsNullOrWhiteSpace(dto.Email) ? user.Email : dto.Email;
        var newAvatarId = dto.AvatarId ?? user.AvatarId;

        if (!string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
        {
            if (await _userRepository.ExistsByUsernameAsync(newUsername))
                throw new InvalidOperationException("Это имя пользователя уже используется другим пользователем.");
        }

        if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (await _userRepository.ExistsByEmailAsync(newEmail))
                throw new InvalidOperationException("Данная почта уже используется другим пользователем.");
        }

        if (dto.AvatarId is not null && dto.AvatarId != user.AvatarId)
        {
            var avatar = await _avatarRepository.GetByIdAsync(dto.AvatarId.Value);
            if (avatar is null || avatar.UserId != userId)
                throw new KeyNotFoundException("Данный аватар не найден.");
        }

        user.UpdateProfile(newUsername, newEmail, newAvatarId);

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(dto.OldPassword))
                throw new ArgumentException("Для смены пароля необходимо указать старый пароль.");

            if (!BCrypt.Net.BCrypt.EnhancedVerify(dto.OldPassword, user.PasswordHash))
                throw new InvalidOperationException("Текущий пароль указан неверно.");

            if (dto.NewPassword.Length < 8)
                throw new ArgumentException("Пароль должен содержать не менее 8 символов.");

            string newHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.NewPassword);
            user.ChangePassword(newHash);
        }

        await _userRepository.UpdateAsync(user);

        return MapToDto(user);
    }

    public async Task<LoginResultDto> AuthenticateAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        if (user is null)
            throw new InvalidOperationException("Неверный логин или пароль");

        if (user.IsLockedOut)
            throw new AccountLockedException("Аккаунт заблокирован из-за превышения лимита попыток входа.");

        if (!BCrypt.Net.BCrypt.EnhancedVerify(dto.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(maxAttempts: 3, lockoutMinutes: 15);
            await _userRepository.UpdateAsync(user);

            if (user.IsLockedOut)
                throw new AccountLockedException("Превышен лимит попыток входа. Аккаунт заблокирован.");

            throw new InvalidOperationException("Неверный логин или пароль");
        }

        user.ResetLoginFailures();

        if (user.IsTwoFactorEnabled)
        {
            var code = user.GenerateTwoFactorCode(expiryMinutes: 5);
            await _userRepository.UpdateAsync(user);

            if (_emailProvider is null)
                throw new InvalidOperationException("Почтовый провайдер для 2FA не настроен.");

            await _emailProvider.SendEmailAsync(
                user.Email,
                "Код подтверждения входа (2FA)",
                $"Ваш одноразовый код подтверждения: {code}");

            return new LoginResultDto(null, RequiresTwoFactor: true, Message: "Код подтверждения отправлен на email.");
        }

        await _userRepository.UpdateAsync(user);
        var token = _jwtProvider.GenerateToken(user, user.Role.ToString());
        return new LoginResultDto(token, RequiresTwoFactor: false);
    }

    public async Task<string> VerifyTwoFactorAsync(VerifyTwoFactorDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username)
            ?? throw new KeyNotFoundException("Пользователь не найден.");

        if (user.IsLockedOut)
            throw new AccountLockedException("Аккаунт заблокирован.");

        if (!user.VerifyTwoFactorCode(dto.Code))
        {
            user.RecordFailedLogin(maxAttempts: 3, lockoutMinutes: 15);
            await _userRepository.UpdateAsync(user);

            if (user.IsLockedOut)
                throw new AccountLockedException("Превышен лимит попыток ввода кода 2FA. Аккаунт заблокирован.");

            throw new InvalidOperationException("Неверный или истекший код подтверждения.");
        }

        user.ResetLoginFailures();
        await _userRepository.UpdateAsync(user);

        return _jwtProvider.GenerateToken(user, user.Role.ToString());
    }

    public async Task RequestUnlockAsync(RequestUnlockDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username)
            ?? throw new KeyNotFoundException("Пользователь не найден.");

        var unlockCode = user.GenerateUnlockCode(expiryMinutes: 15);
        await _userRepository.UpdateAsync(user);

        if (_emailProvider is null)
            throw new InvalidOperationException("Почтовый провайдер не настроен.");

        await _emailProvider.SendEmailAsync(
            user.Email,
            "Восстановление доступа к аккаунту",
            $"Ваш код для разблокировки аккаунта: {unlockCode}");
    }

    public async Task ConfirmUnlockAsync(ConfirmUnlockDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username)
            ?? throw new KeyNotFoundException("Пользователь не найден.");

        if (!user.TryUnlock(dto.Code))
            throw new InvalidOperationException("Неверный или истекший код разблокировки.");

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            if (dto.NewPassword.Length < 8)
                throw new ArgumentException("Пароль должен содержать не менее 8 символов.");

            var newHash = BCrypt.Net.BCrypt.EnhancedHashPassword(dto.NewPassword);
            user.ChangePassword(newHash);
        }

        await _userRepository.UpdateAsync(user);
    }

    public async Task SetTwoFactorAsync(Guid userId, bool enabled)
    {
        var user = await GetUserOrThrowAsync(userId);
        if (enabled)
            user.EnableTwoFactor();
        else
            user.DisableTwoFactor();

        await _userRepository.UpdateAsync(user);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            TeamId = user.TeamId ?? Guid.Empty,
            AvatarId = user.AvatarId
        };
    }
}