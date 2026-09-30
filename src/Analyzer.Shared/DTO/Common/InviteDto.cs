namespace Analyzer.Shared.DTO.Common;

using Analyzer.Domain.Enums;

public class GenerateInviteDto
{
    public string Email { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
    public int ValidForDays { get; set; }
    public Role Role { get; set; }
}

public record CreateInviteDto(string Email, int ValidForDays, Role Role);

public record InviteDto(Guid Id, 
                        Role Role, 
                        string TargetEmail,
                        string Code, 
                        DateTimeOffset ExpirationDate, 
                        InviteStatus Status);
