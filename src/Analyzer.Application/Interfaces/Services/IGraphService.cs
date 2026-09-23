namespace Analyzer.Application.Interfaces.Services;

using Analyzer.Shared.DTO;

public interface IGraphService
{
    Task<IReadOnlyCollection<ComponentDto>> GetComponentsBySystemIdAsync(Guid systemId);
    Task<ComponentDto> GetComponentDetailsAsync(Guid id);
    Task<Guid> CreateComponentAsync(CreateComponentDto dto);
    Task<ComponentDto> UpdateComponentAsync(Guid id, UpdateComponentDto dto);
    Task<ComponentDto> PatchComponentAsync(Guid id, PatchComponentDto dto);
    Task DeleteComponentAsync(Guid id);

    Task<IReadOnlyCollection<LinkDto>> GetLinksBySystemIdAsync(Guid systemId);
    Task<Guid> CreateLinkAsync(CreateLinkDto dto);
    Task DeleteLinkAsync(Guid id);
}