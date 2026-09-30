namespace Analyzer.Api.Controllers.V1;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v1/links")]
public class LinkController(IGraphService graphService) : ControllerBase
{
    private readonly IGraphService _graphService = graphService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<LinkDto>>> GetAllLinksBySystemId([FromQuery] Guid systemId)
    {
        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        return Ok(links);
    }

    [HttpGet("~/api/v1/systems/{systemId:guid}/links")]
    public async Task<ActionResult<IReadOnlyCollection<LinkDto>>> GetLinksBySystem(Guid systemId)
    {
        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        return Ok(links);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkDto linkDto)
    {
        var id = await _graphService.CreateLinkAsync(linkDto);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLink(Guid id)
    {
        await _graphService.DeleteLinkAsync(id);
        return NoContent();
    }
}