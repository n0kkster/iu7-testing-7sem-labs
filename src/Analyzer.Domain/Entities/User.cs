using Analyzer.Domain.Enums;
using System.Security.Cryptography;

namespace Analyzer.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Role Role { get; private set; }
    public Guid? TeamId { get; private set; }
    public Guid? AvatarId { get; private set; } = null;

    public bool IsTwoFactorEnabled { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public string? TwoFactorCode { get; private set; }
    public DateTimeOffset? TwoFactorCodeExpiry { get; private set; }
    public string? UnlockCode { get; private set; }
    public DateTimeOffset? UnlockCodeExpiry { get; private set; }
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    private User(Guid id, string username, string email, string passwordHash, Role role, Guid? teamId, Guid? avatarId)
    {
        Id = id;

        Username = string.IsNullOrWhiteSpace(username)
                ? throw new ArgumentException("Имя пользователя обязательно")
                : username;

        Email = string.IsNullOrWhiteSpace(email)
                ? throw new ArgumentException("Email обязателен")
                : !IsValidEmail(email)
                ? throw new ArgumentException("Email невалиден")
                : email;

        PasswordHash = string.IsNullOrWhiteSpace(passwordHash)
                ? throw new ArgumentException("Хэш пароля обязателен")
                : passwordHash;

        Role = role;
        TeamId = teamId;
        AvatarId = avatarId;
    }

    // Статическая фабрика жи есть
    public static User CreateInvitedUser(string username, string email, string passwordHash, Role role, Guid teamId)
    {
        if (role == Role.Admin)
            throw new InvalidOperationException("Пользователь из команды не может быть администратором.");

        if (teamId == Guid.Empty)
            throw new ArgumentException("Идентификатор команды обязателен для приглашенного пользователя.");

        return new User(Guid.NewGuid(), username, email, passwordHash, role, teamId, null);
    }

    public static User CreateAdmin(string username, string email, string passwordHash)
    {
        return new User(Guid.NewGuid(), username, email, passwordHash, Role.Admin, null, null);
    }

    public static User Restore(Guid id, string username, string email, string passwordHash, Role role, Guid? teamId, Guid? avatarId)
    {
        return new User(id, username, email, passwordHash, role, teamId, avatarId);
    }

    public void UpdateProfile(string username, string email, Guid? newAvatarId)
    {
        Username = string.IsNullOrWhiteSpace(username)
                 ? throw new ArgumentException("Имя пользователя обязательно")
                 : username;

        Email = string.IsNullOrWhiteSpace(email)
              ? throw new ArgumentException("Email обязателен")
              : !IsValidEmail(email)
              ? throw new ArgumentException("Email невалиден")
              : email;

        if (newAvatarId is not null)
            AvatarId = newAvatarId;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Новый хэш пароля не может быть пустым");

        PasswordHash = newPasswordHash;
    }

    private bool IsValidEmail(string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
            return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }

    public void EnableTwoFactor() => IsTwoFactorEnabled = true;

    public void DisableTwoFactor()
    {
        IsTwoFactorEnabled = false;
        TwoFactorCode = null;
        TwoFactorCodeExpiry = null;
    }

    public void RecordFailedLogin(int maxAttempts = 3, int lockoutMinutes = 15)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
        }
    }

    public void ResetLoginFailures()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public string GenerateTwoFactorCode(int expiryMinutes = 5)
    {
        TwoFactorCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        TwoFactorCodeExpiry = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        return TwoFactorCode;
    }

    public bool VerifyTwoFactorCode(string code)
    {
        if (string.IsNullOrWhiteSpace(TwoFactorCode) ||
            TwoFactorCodeExpiry is null ||
            TwoFactorCodeExpiry <= DateTimeOffset.UtcNow ||
            !string.Equals(TwoFactorCode, code, StringComparison.Ordinal))
        {
            return false;
        }

        TwoFactorCode = null;
        TwoFactorCodeExpiry = null;
        return true;
    }

    public string GenerateUnlockCode(int expiryMinutes = 15)
    {
        UnlockCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        UnlockCodeExpiry = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        return UnlockCode;
    }

    public bool TryUnlock(string code)
    {
        if (string.IsNullOrWhiteSpace(UnlockCode) ||
            UnlockCodeExpiry is null ||
            UnlockCodeExpiry <= DateTimeOffset.UtcNow ||
            !string.Equals(UnlockCode, code, StringComparison.Ordinal))
        {
            return false;
        }

        ResetLoginFailures();
        UnlockCode = null;
        UnlockCodeExpiry = null;
        return true;
    }

    public void LoadAuthState(
        bool isTwoFactorEnabled,
        int failedLoginAttempts,
        DateTimeOffset? lockoutEnd,
        string? twoFactorCode,
        DateTimeOffset? twoFactorCodeExpiry,
        string? unlockCode,
        DateTimeOffset? unlockCodeExpiry)
    {
        IsTwoFactorEnabled = isTwoFactorEnabled;
        FailedLoginAttempts = failedLoginAttempts;
        LockoutEnd = lockoutEnd;
        TwoFactorCode = twoFactorCode;
        TwoFactorCodeExpiry = twoFactorCodeExpiry;
        UnlockCode = unlockCode;
        UnlockCodeExpiry = unlockCodeExpiry;
    }
}