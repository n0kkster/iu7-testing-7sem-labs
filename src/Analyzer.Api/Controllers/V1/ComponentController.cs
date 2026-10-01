namespace Analyzer.Api.Controllers.V1;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v1/components")]
public class ComponentController(IGraphService graphService) : ControllerBase
{
    readonly IGraphService _graphService = graphService;

    [HttpGet]
    public async Task<IActionResult> GetAllComponentsBySystemId([FromQuery] Guid systemId)
    {
        var componentDtos = await _graphService.GetComponentsBySystemIdAsync(systemId);
        return Ok(componentDtos);
    }

    [Authorize (Roles = "Architect")]
    [HttpPost]
    public async Task<IActionResult> CreateComponent([FromBody] CreateComponentDto dto)
    {
        var guid = await _graphService.CreateComponentAsync(dto);
        return Ok(guid);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetComponentDetails(Guid id)
    {
        var componentDto = await _graphService.GetComponentDetailsAsync(id);
        return Ok(componentDto);
    }

    [Authorize (Roles = "Architect")]
    [HttpPut]
    public async Task<IActionResult> UpdateComponent([FromBody] ComponentDto dto)
    {
        var updateDto = new UpdateComponentDto(dto.Name, dto.Description, dto.Type);
        await _graphService.UpdateComponentAsync(dto.Id, updateDto);
        return Ok();
    }

    [Authorize (Roles = "Architect")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComponent(Guid id)
    {
        await _graphService.DeleteComponentAsync(id);
        return Ok();
    }
}