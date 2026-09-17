namespace Analyzer.Domain.Entities;

using Analyzer.Domain.Enums;

public class Link
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid SourceId { get; init; }
    public required Guid TargetId { get; init; }
    public required LinkSeverity Severity { get; set; }
    public required ProtocolType Protocol { get; set; }
}