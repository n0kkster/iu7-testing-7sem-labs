using Analyzer.Domain.Enums;

namespace Analyzer.Shared.DTO;

public class GenerateInviteDto
{
    public string Email { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
    public int ValidForDays { get; set; }
    public Role Role { get; set; }
}

public record InviteDto(Guid Id, 
                        Role Role, 
                        string TargetEmail,
                        string Code, 
                        DateTimeOffset ExpirationDate, 
                        InviteStatus Status);
