namespace Analyzer.Shared.DTO;

using Analyzer.Domain.Enums;

public record CreateLinkDto(Guid SourceId, Guid TargetId, LinkSeverity Severity, ProtocolType Protocol);
public record LinkDto
{
    public required Guid Id { get; init; }
    public required Guid SourceId { get; init; }
    public required Guid TargetId { get; init; }
    public required LinkSeverity Severity { get; set; }
    public required ProtocolType Protocol { get; set; }
}
