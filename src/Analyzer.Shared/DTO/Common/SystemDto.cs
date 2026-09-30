namespace Analyzer.Shared.DTO.Common;

public record ITSystemDto(
    Guid Id,
    string Name,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid TeamId,
    int ComponentsCount);

public class CreateITSystemDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
}

public record UpdateITSystemDto(string Name, string Description);

public record PatchITSystemDto(string? Name, string? Description);