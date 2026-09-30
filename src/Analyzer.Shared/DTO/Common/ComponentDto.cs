namespace Analyzer.Shared.DTO.Common;

using Analyzer.Domain.Enums;

public record CreateComponentDto(Guid SystemId, ComponentType Type, string Name, string Description);

public record UpdateComponentDto(string Name, string Description, ComponentType Type);

public record PatchComponentDto(string? Name, string? Description, ComponentType? Type);

public record ComponentDto
{
    public required Guid Id { get; init; }
    public required Guid SystemId { get; init; }
    public required ComponentType Type { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}