namespace Analyzer.Api.Controllers.V2;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v2/components")]
public class ComponentController(IGraphService graphService) : ControllerBase
{
    private readonly IGraphService _graphService = graphService;

    [HttpGet("{id:guid}")]
    [ActionName("getComponentById")]
    [ProducesResponseType(typeof(ComponentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComponentDto>> GetComponentById(Guid id)
    {
        var component = await _graphService.GetComponentDetailsAsync(id);
        return Ok(component);
    }

    [Authorize(Roles = "Architect")]
    [HttpPost]
    [ActionName("createComponent")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateComponent([FromBody] CreateComponentDto dto)
    {
        var id = await _graphService.CreateComponentAsync(dto);

        return CreatedAtAction(
            nameof(GetComponentById), 
            new { id }, 
            new { id });
    }

    [Authorize(Roles = "Architect")]
    [HttpPut("{id:guid}")]
    [ActionName("updateComponent")]
    [ProducesResponseType(typeof(ComponentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComponentDto>> UpdateComponent(Guid id, [FromBody] UpdateComponentDto dto)
    {
        var updated = await _graphService.UpdateComponentAsync(id, dto);
        return Ok(updated);
    }

    [Authorize(Roles = "Architect")]
    [HttpPatch("{id:guid}")]
    [ActionName("patchComponent")]
    [ProducesResponseType(typeof(ComponentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComponentDto>> PatchComponent(Guid id, [FromBody] PatchComponentDto dto)
    {
        var patched = await _graphService.PatchComponentAsync(id, dto);
        return Ok(patched);
    }

    [Authorize(Roles = "Architect")]
    [HttpDelete("{id:guid}")]
    [ActionName("deleteComponent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComponent(Guid id)
    {
        await _graphService.DeleteComponentAsync(id);
        return NoContent();
    }
}