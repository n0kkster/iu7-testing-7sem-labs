namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO.Common;

public class SystemService(
    IGraphService graphService, 
    ISystemRepository systemsRepository) : ISystemService
{
    private readonly IGraphService _graphService = graphService;
    private readonly ISystemRepository _systemsRepository = systemsRepository;

    public async Task<ITSystemDto> GetSystemByIdAsync(Guid systemId)
    {
        var system = await _systemsRepository.GetByIdAsync(systemId)
            ?? throw new KeyNotFoundException($"Система с ID {systemId} не найдена.");

        var components = await _graphService.GetComponentsBySystemIdAsync(system.Id);

        return new ITSystemDto(
            system.Id,
            system.Name,
            system.Description,
            system.CreatedAt,
            system.UpdatedAt,
            system.TeamId,
            components.Count
        );
    }

    public async Task<IReadOnlyCollection<ITSystemDto>> GetSystemsByTeamIdAsync(Guid teamId)
    {
        var systems = await _systemsRepository.GetByTeamIdAsync(teamId);
        var tasks = systems.Select(async system =>
        {
            var components = await _graphService.GetComponentsBySystemIdAsync(system.Id);
            return new { system.Id, components.Count };
        });

        var results = await Task.WhenAll(tasks);
        var counts = results.ToDictionary(x => x.Id, x => x.Count);

        return systems.Select(system => new ITSystemDto(
            system.Id,
            system.Name,
            system.Description,
            system.CreatedAt,
            system.UpdatedAt,
            system.TeamId,
            counts[system.Id]
        )).ToList();
    }

    public async Task<Guid> CreateSystemAsync(CreateITSystemDto dto)
    {
        var system = new ITSystem(dto.Name, dto.Description, dto.TeamId);
        await _systemsRepository.AddAsync(system);
        return system.Id;
    }

    public async Task UpdateSystemAsync(Guid id, UpdateITSystemDto dto)
    {
        var system = await _systemsRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Система с ID {id} не найдена.");

        system.UpdateDetails(dto.Name, dto.Description);
        await _systemsRepository.UpdateAsync(system);
    }

    public async Task<ITSystemDto> PatchSystemAsync(Guid id, PatchITSystemDto dto)
    {
        var system = await _systemsRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Система с ID {id} не найдена.");

        var newName = string.IsNullOrWhiteSpace(dto.Name) ? system.Name : dto.Name;
        var newDesc = dto.Description ?? system.Description;

        system.UpdateDetails(newName, newDesc);
        await _systemsRepository.UpdateAsync(system);

        return await GetSystemByIdAsync(id);
    }

    public async Task DeleteSystemAsync(Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        foreach (var component in components)
            await _graphService.DeleteComponentAsync(component.Id);

        await _systemsRepository.DeleteAsync(systemId);
    }

    public async Task<(IReadOnlyCollection<ComponentDto>, IReadOnlyCollection<LinkDto>)> ExportSystemAsync(Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        return (components, links);
    }

    public async Task<Guid> ImportSystemAsync(
        IReadOnlyCollection<ComponentDto> components,
        IReadOnlyCollection<LinkDto> links,
        CreateITSystemDto systemDto)
    {
        var guidMap = new Dictionary<Guid, Guid>();
        var newSystemId = await CreateSystemAsync(systemDto);

        foreach (var component in components)
        {
            var newGuid = await _graphService.CreateComponentAsync(new(
                newSystemId,
                component.Type,
                component.Name,
                component.Description
            ));
            guidMap.Add(component.Id, newGuid);
        }

        foreach (var link in links)
        {
            if (guidMap.TryGetValue(link.SourceId, out var newSourceId) &&
                guidMap.TryGetValue(link.TargetId, out var newTargetId))
            {
                await _graphService.CreateLinkAsync(new CreateLinkDto(
                    newSourceId,
                    newTargetId,
                    link.Severity,
                    link.Protocol
                ));
            }
        }

        return newSystemId;
    }
}