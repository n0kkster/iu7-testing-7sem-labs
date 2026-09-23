namespace Analyzer.Application.Interfaces.Services;

using Analyzer.Shared.DTO;

public interface ISystemService
{
    Task<ITSystemDto> GetSystemByIdAsync(Guid systemId);
    Task<IReadOnlyCollection<ITSystemDto>> GetSystemsByTeamIdAsync(Guid teamId);
    Task<Guid> CreateSystemAsync(CreateITSystemDto dto);
    Task UpdateSystemAsync(Guid id, UpdateITSystemDto dto);
    Task<ITSystemDto> PatchSystemAsync(Guid id, PatchITSystemDto dto);
    Task DeleteSystemAsync(Guid systemId);

    Task<(IReadOnlyCollection<ComponentDto>, IReadOnlyCollection<LinkDto>)> ExportSystemAsync(Guid systemId);
    Task<Guid> ImportSystemAsync(
        IReadOnlyCollection<ComponentDto> components,
        IReadOnlyCollection<LinkDto> links,
        CreateITSystemDto systemDto);
}