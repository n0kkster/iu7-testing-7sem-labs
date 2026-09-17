namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO;

public class SystemService(IGraphService graphService, 
                           ISystemRepository systemsRepository) : ISystemService
{
    readonly IGraphService _graphService = graphService;
    readonly ISystemRepository _systemsRepository = systemsRepository;

    public async Task<IReadOnlyCollection<ITSystemDto>> GetSystemsByTeamIdAsync(Guid teamId)
    {
        var systems = await _systemsRepository.GetByTeamIdAsync(teamId);
        var tasks = systems.Select(async system =>
        {
            var components = await _graphService.GetComponentsBySystemIdAsync(system.Id);
            return new { system.Id, Count = components.Count() };
        });

        var results = await Task.WhenAll(tasks);
        var systemsComponentsCount = results.ToDictionary(x => x.Id, x => x.Count);

        return systems.Select(system => new ITSystemDto(
            system.Id,
            system.Name,
            system.Description,
            system.CreatedAt,
            system.UpdatedAt,
            system.TeamId,
            systemsComponentsCount[system.Id]
        )).ToList();
    }

    public async Task<(IReadOnlyCollection<ComponentDto>, IReadOnlyCollection<LinkDto>)> ExportSystemAsync(
        Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        return (components, links);
    }

    public async Task<Guid> ImportSystemAsync(IReadOnlyCollection<ComponentDto> components,
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
                var newLink = new CreateLinkDto(
                    newSourceId,
                    newTargetId,
                    link.Severity,
                    link.Protocol
                );

                await _graphService.CreateLinkAsync(newLink);
            }
            else
            {
                Console.WriteLine($"Откуда-то взялась битая ссылка? ({link.SourceId} -> {link.TargetId})");
            }
        }

        return newSystemId;
    }

    public async Task<Guid> CreateSystemAsync(CreateITSystemDto dto)
    {
        var newSystem = new ITSystem(dto.Name, dto.Description, dto.TeamId);
        await _systemsRepository.AddAsync(newSystem);

        return newSystem.Id;
    }

    public async Task UpdateSystemAsync(ITSystemDto dto)
    {
        var system = await _systemsRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("Система не найдена");

        system.UpdateDetails(dto.Name, dto.Description);
    }

    public async Task DeleteSystemAsync(Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        foreach (var component in components)
            await _graphService.DeleteComponentAsync(component.Id);

        await _systemsRepository.DeleteAsync(systemId);
    }
}