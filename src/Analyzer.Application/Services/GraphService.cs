namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Shared.DTO;
using Analyzer.Domain.Entities;

public class GraphService(IGraphRepository repository) : IGraphService
{
    private readonly IGraphRepository _repository = repository;

    public async Task<IReadOnlyCollection<ComponentDto>> GetComponentsBySystemIdAsync(Guid systemId)
    {
        var components = await _repository.GetComponentsBySystemIdAsync(systemId);
        return components.Select(MapToDto).ToList();
    }

    public async Task<ComponentDto> GetComponentDetailsAsync(Guid id)
    {
        var component = await _repository.GetComponentAsync(id);
        return MapToDto(component);
    }

    public async Task<Guid> CreateComponentAsync(CreateComponentDto dto)
    {
        var component = new Component
        {
            Type = dto.Type,
            Name = dto.Name,
            Description = dto.Description,
            SystemId = dto.SystemId
        };

        await _repository.AddComponentAsync(component);
        return component.Id;
    }

    public async Task<ComponentDto> UpdateComponentAsync(Guid id, UpdateComponentDto dto)
    {
        var existing = await _repository.GetComponentAsync(id);

        var updated = new Component
        {
            Id = id,
            SystemId = existing.SystemId,
            Type = dto.Type,
            Name = dto.Name,
            Description = dto.Description
        };

        await _repository.UpdateComponentAsync(updated);
        return MapToDto(updated);
    }

    public async Task<ComponentDto> PatchComponentAsync(Guid id, PatchComponentDto dto)
    {
        var component = await _repository.GetComponentAsync(id);

        if (!string.IsNullOrWhiteSpace(dto.Name))
            component.Name = dto.Name;

        if (dto.Description is not null)
            component.Description = dto.Description;

        await _repository.UpdateComponentAsync(component);
        return MapToDto(component);
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        await _repository.DeleteComponentAsync(id);
    }

    public async Task<Guid> CreateLinkAsync(CreateLinkDto dto)
    {
        var link = new Link
        {
            SourceId = dto.SourceId,
            TargetId = dto.TargetId,
            Severity = dto.Severity,
            Protocol = dto.Protocol
        };

        await _repository.AddLinkAsync(link);
        return link.Id;
    }

    public async Task<IReadOnlyCollection<LinkDto>> GetLinksBySystemIdAsync(Guid systemId)
    {
        var links = await _repository.GetLinksBySystemIdAsync(systemId);
        return links.Select(link => new LinkDto
        {
            Id = link.Id,
            SourceId = link.SourceId,
            TargetId = link.TargetId,
            Severity = link.Severity,
            Protocol = link.Protocol
        }).ToList();
    }

    public async Task DeleteLinkAsync(Guid id)
    {
        await _repository.DeleteLinkAsync(id);
    }

    private static ComponentDto MapToDto(Component c) => new()
    {
        Id = c.Id,
        SystemId = c.SystemId,
        Type = c.Type,
        Name = c.Name,
        Description = c.Description
    };
}