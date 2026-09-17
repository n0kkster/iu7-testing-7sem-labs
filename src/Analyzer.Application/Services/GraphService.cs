namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Shared.DTO;
using Analyzer.Domain.Exceptions;
using Analyzer.Domain.Entities;

public class GraphService(IGraphRepository repository) : IGraphService
{
    readonly IGraphRepository _repository = repository;

    public async Task<IReadOnlyCollection<ComponentDto>> GetComponentsBySystemIdAsync(Guid systemId)
    {
        var components = await _repository.GetComponentsBySystemIdAsync(systemId);
        var componentDtos = components.Select(component => new ComponentDto() {
            Id = component.Id,
            SystemId = component.SystemId,
            Type = component.Type,
            Name = component.Name,
            Description = component.Description
        }).ToList();

        return componentDtos;
    }

    public async Task<ComponentDto> GetComponentDetailsAsync(Guid id)
    {
        var component = await _repository.GetComponentAsync(id);
        ComponentDto componentDto = new()
        {
            Id = component.Id,
            SystemId = component.SystemId,
            Type = component.Type,
            Name = component.Name,
            Description = component.Description
        };

        return componentDto;
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

    public async Task UpdateComponentAsync(ComponentDto dto)
    {
        var component = new Component
        {
            Id = dto.Id,
            Type = dto.Type,
            Name = dto.Name,
            Description = dto.Description,
            SystemId = dto.SystemId
        };

        await _repository.UpdateComponentAsync(component);
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        await _repository.DeleteComponentAsync(id);
    }

    public async Task<Guid> CreateLinkAsync(CreateLinkDto dto)
    {
        var (sourceId, targetId, severity, protocol) = dto;
        var link = new Link
        {
            SourceId = sourceId,
            TargetId = targetId,
            Severity = severity,
            Protocol = protocol
        };
        await _repository.AddLinkAsync(link);
        return link.Id;
    }
    public async Task<IReadOnlyCollection<LinkDto>> GetLinksBySystemIdAsync(Guid systemId)
    {
        var links = await _repository.GetLinksBySystemIdAsync(systemId);
        var linkDtos = links.Select(link => new LinkDto() {
            Id = link.Id,
            SourceId = link.SourceId,
            TargetId = link.TargetId,
            Severity = link.Severity,
            Protocol = link.Protocol
        }).ToList();

        return linkDtos;
    }

    public async Task DeleteLinkAsync(Guid id)
    {
        await _repository.DeleteLinkAsync(id);
    }
}