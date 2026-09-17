namespace Analyzer.Application.Interfaces.Services;

using Analyzer.Shared.DTO;

public interface IGraphService
{
    Task<IReadOnlyCollection<ComponentDto>> GetComponentsBySystemIdAsync(Guid systemId);
    Task<ComponentDto> GetComponentDetailsAsync(Guid id);
    Task<Guid> CreateComponentAsync(CreateComponentDto dto);
    Task UpdateComponentAsync(ComponentDto dto);
    Task DeleteComponentAsync(Guid id);

    Task<IReadOnlyCollection<LinkDto>> GetLinksBySystemIdAsync(Guid systemId);
    Task<Guid> CreateLinkAsync(CreateLinkDto dto);
    Task DeleteLinkAsync(Guid id);
}