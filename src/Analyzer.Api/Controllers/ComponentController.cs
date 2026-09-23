namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v1/components")]
public class ComponentController(IGraphService graphService) : ControllerBase
{
    private readonly IGraphService _graphService = graphService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ComponentDto>>> GetComponents([FromQuery] Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        return Ok(components);
    }

    [HttpGet("~/api/v1/systems/{systemId:guid}/components")]
    public async Task<ActionResult<IReadOnlyCollection<ComponentDto>>> GetComponentsBySystem(Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        return Ok(components);
    }

    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetComponentById))]
    public async Task<ActionResult<ComponentDto>> GetComponentById(Guid id)
    {
        var component = await _graphService.GetComponentDetailsAsync(id);
        return Ok(component);
    }

    [Authorize(Roles = "Architect")]
    [HttpPost]
    public async Task<IActionResult> CreateComponent([FromBody] CreateComponentDto dto)
    {
        var id = await _graphService.CreateComponentAsync(dto);
        return CreatedAtAction(nameof(GetComponentById), new { id }, new { id });
    }

    [Authorize(Roles = "Architect")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateComponent(Guid id, [FromBody] UpdateComponentDto dto)
    {
        var updated = await _graphService.UpdateComponentAsync(id, dto);
        return Ok(updated);
    }

    [Authorize(Roles = "Architect")]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchComponent(Guid id, [FromBody] PatchComponentDto dto)
    {
        var patched = await _graphService.PatchComponentAsync(id, dto);
        return Ok(patched);
    }

    [Authorize(Roles = "Architect")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteComponent(Guid id)
    {
        await _graphService.DeleteComponentAsync(id);
        return NoContent();
    }
}