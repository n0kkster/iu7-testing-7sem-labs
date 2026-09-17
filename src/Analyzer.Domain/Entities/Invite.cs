using Analyzer.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Analyzer.Domain.Entities;

public class Invite
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public InviteStatus Status
    {
        get
        {
            if (field == InviteStatus.Pending && DateTimeOffset.UtcNow > ExpirationDate)
                field = InviteStatus.Expired;

            return field;
        }
        private set;
    }

    public string TargetEmail { get; init; }
    public Role Role { get; init; }
    public string Code { get; init; }
    public DateTimeOffset ExpirationDate { get; private set; }
    public Guid TeamId { get; init; }
    public Guid? ActivatedByUserId { get; private set; }

    // Давим варнинг, потому что данный конструктор нужен только для EF.Core
#pragma warning disable CS8618
    private Invite() { }
#pragma warning restore CS8618

    public Invite(string targetEmail, int validForDays, Guid teamId, Role role)
    {
        TeamId = teamId == Guid.Empty ?
            throw new ArgumentException("Команда обязательна") :
            teamId;

        TargetEmail = string.IsNullOrWhiteSpace(targetEmail)
              ? throw new ArgumentException("Email обязателен")
              : !IsValidEmail(targetEmail) 
              ? throw new ArgumentException("Email невалиден")
              : targetEmail;

        Role = role;
        Status = InviteStatus.Pending;

        Code = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(targetEmail)));

        ExpirationDate = DateTimeOffset.UtcNow.AddDays(validForDays);
    }

    private bool CheckTarget(string targetEmail)
    {
        return Code == Convert.ToHexString(
                            SHA256.HashData(
                                Encoding.UTF8.GetBytes(targetEmail)));
    }

    public void ValidateCanBeConsumedBy(string targetEmail)
    {
        if (!CheckTarget(targetEmail))
            throw new ArgumentException("Приглашение не предназначено для этого пользователя");

        if (Status == InviteStatus.Expired)
            throw new InvalidOperationException("Приглашение истекло");

        if (Status == InviteStatus.Activated)
            throw new InvalidOperationException("Приглашение уже активировано");

        if (Status == InviteStatus.Revoked)
            throw new InvalidOperationException("Приглашение было отозвано");
    }

    public (Role Role, Guid TeamId) GetDetails()
    {
        return (Role, TeamId);
    }

    public void Consume(Guid newUserId)
    {
        if (Status != InviteStatus.Pending)
            throw new InvalidOperationException("Инвайт не находится в статусе ожидания");

        Status = InviteStatus.Activated;
        ActivatedByUserId = newUserId;
    }

    public void Revoke()
    {
        if (Status == InviteStatus.Activated)
            throw new InvalidOperationException("Нельзя отозвать уже принятое приглашение");

        Status = InviteStatus.Revoked;
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