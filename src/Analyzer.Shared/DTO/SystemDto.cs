namespace Analyzer.Shared.DTO;

public record ITSystemDto(Guid Id,
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