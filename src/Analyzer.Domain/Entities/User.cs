using Analyzer.Domain.Enums;

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

    // Для EF Core
#pragma warning disable CS8618
    private User() { }
#pragma warning restore CS8618

    private User(string username, string email, string passwordHash, Role role, Guid? teamId)
    {
        Id = Guid.NewGuid();

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
    }

    // Статическая фабрика жи есть
    public static User CreateInvitedUser(string username, string email, string passwordHash, Role role, Guid teamId)
    {
        if (role == Role.Admin)
            throw new InvalidOperationException("Пользователь из команды не может быть администратором.");

        if (teamId == Guid.Empty)
            throw new ArgumentException("Идентификатор команды обязателен для приглашенного пользователя.");

        return new User(username, email, passwordHash, role, teamId);
    }

    public static User CreateAdmin(string username, string email, string passwordHash)
    {
        return new User(username, email, passwordHash, Role.Admin, null);
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
}