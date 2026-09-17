namespace Analyzer.Application.Interfaces.Services;

using Analyzer.Shared.DTO;

public interface ISystemService
{
    Task<(IReadOnlyCollection<ComponentDto>, IReadOnlyCollection<LinkDto>)> ExportSystemAsync(Guid systemId);
    Task<Guid> ImportSystemAsync(IReadOnlyCollection<ComponentDto> components, 
                            IReadOnlyCollection<LinkDto> links,
                            CreateITSystemDto systemDto);

    Task<IReadOnlyCollection<ITSystemDto>> GetSystemsByTeamIdAsync(Guid teamId);
    
    public Task<Guid> CreateSystemAsync(CreateITSystemDto dto); 
    public Task UpdateSystemAsync(ITSystemDto dto);
    public Task DeleteSystemAsync(Guid systemId);
}