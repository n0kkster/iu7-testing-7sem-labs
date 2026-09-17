namespace Analyzer.Api.Controllers;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v1/links")]
public class LinkController(IGraphService graphService) : ControllerBase
{
    readonly IGraphService _graphService = graphService;

    [HttpGet]
    public async Task<IActionResult> GetAllLinksBySystemId([FromQuery] Guid systemId)
    {
        var componentDtos = await _graphService.GetLinksBySystemIdAsync(systemId);
        return Ok(componentDtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkDto linkDto)
    {
        var guid = await _graphService.CreateLinkAsync(linkDto);
        return Ok(guid);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLink(Guid id)
    {
        await _graphService.DeleteLinkAsync(id);
        return Ok();
    }
}