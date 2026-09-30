namespace Analyzer.Api.Controllers.V2;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v2/links")]
public class LinkController(IGraphService graphService) : ControllerBase
{
    private readonly IGraphService _graphService = graphService;

    [HttpPost]
    [ActionName("createLink")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkDto linkDto)
    {
        var id = await _graphService.CreateLinkAsync(linkDto);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpDelete("{id:guid}")]
    [ActionName("deleteLink")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLink(Guid id)
    {
        await _graphService.DeleteLinkAsync(id);
        return NoContent();
    }
}