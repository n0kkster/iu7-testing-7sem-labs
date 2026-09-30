namespace Analyzer.Api.Controllers.V2;

using System.Text;
using System.Text.Json;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v2/systems")]
public class SystemsController(
    ISystemService systemsService, 
    IGraphService graphService) : ControllerBase
{
    private readonly ISystemService _systemsService = systemsService;
    private readonly IGraphService _graphService = graphService;

    public record SystemStorage(
        IReadOnlyCollection<ComponentDto> Components,
        IReadOnlyCollection<LinkDto> Links);

    #region CRUD систем

    [HttpGet]
    [ActionName("getSystemsByTeam")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ITSystemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<ITSystemDto>>> ListSystemsByTeam([FromQuery] Guid teamId)
    {
        var systems = await _systemsService.GetSystemsByTeamIdAsync(teamId);
        return Ok(systems);
    }

    [HttpGet("{id:guid}")]
    [ActionName("getSystemById")]
    [ProducesResponseType(typeof(ITSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ITSystemDto>> GetSystemById(Guid id)
    {
        var system = await _systemsService.GetSystemByIdAsync(id);
        return Ok(system);
    }

    [Authorize(Roles = "Architect")]
    [HttpPost]
    [ActionName("createSystem")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSystem([FromBody] CreateITSystemDto dto)
    {
        var createdId = await _systemsService.CreateSystemAsync(dto);

        return CreatedAtAction(
            nameof(GetSystemById), 
            new { id = createdId }, 
            new { id = createdId });
    }

    [Authorize(Roles = "Architect")]
    [HttpPut("{id:guid}")]
    [ActionName("updateSystem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSystem(Guid id, [FromBody] UpdateITSystemDto dto)
    {
        await _systemsService.UpdateSystemAsync(id, dto);
        return NoContent();
    }

    [Authorize(Roles = "Architect")]
    [HttpPatch("{id:guid}")]
    [ActionName("patchSystem")]
    [ProducesResponseType(typeof(ITSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ITSystemDto>> PatchSystem(Guid id, [FromBody] PatchITSystemDto dto)
    {
        var updated = await _systemsService.PatchSystemAsync(id, dto);
        return Ok(updated);
    }

    [Authorize(Roles = "Architect")]
    [HttpDelete("{id:guid}")]
    [ActionName("deleteSystem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSystem(Guid id)
    {
        await _systemsService.DeleteSystemAsync(id);
        return NoContent();
    }

    #endregion

    #region Вложенные ресурсы

    [HttpGet("{systemId:guid}/components")]
    [ActionName("getComponentsBySystemId")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ComponentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<ComponentDto>>> GetComponents(Guid systemId)
    {
        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        return Ok(components);
    }

    [HttpGet("{systemId:guid}/links")]
    [ActionName("getLinksBySystemId")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LinkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<LinkDto>>> GetLinks(Guid systemId)
    {
        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        return Ok(links);
    }

    #endregion

    #region Экспорт и Импорт

    [HttpGet("{id:guid}/export")]
    [ActionName("exportSystem")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportSystem(Guid id)
    {
        var (components, links) = await _systemsService.ExportSystemAsync(id);
        var jsonString = JsonSerializer.Serialize(new SystemStorage(components, links));
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var fileName = $"system-backup-{DateTime.UtcNow:yyyy-MM-dd_HH-mm}.json";

        return File(bytes, "application/json", fileName);
    }

    [Authorize(Roles = "Architect")]
    [HttpPost("import")]
    [ActionName("importSystem")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ImportSystem([FromForm] IFormFile file, [FromForm] string importData)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации данных",
                Detail = "Файл импорта не предоставлен или пуст",
                Instance = HttpContext.Request.Path
            });
        }

        var dto = JsonSerializer.Deserialize<CreateITSystemDto>(
            importData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (dto is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации данных",
                Detail = "Некорректные параметры новой системы (importData)",
                Instance = HttpContext.Request.Path
            });
        }

        await using var stream = file.OpenReadStream();
        var storage = await JsonSerializer.DeserializeAsync<SystemStorage>(stream);

        if (storage is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации данных",
                Detail = "Ошибка десериализации топологии системы",
                Instance = HttpContext.Request.Path
            });
        }

        var newSystemId = await _systemsService.ImportSystemAsync(storage.Components, storage.Links, dto);

        return CreatedAtAction(
            nameof(GetSystemById), 
            new { id = newSystemId }, 
            new { id = newSystemId });
    }

    #endregion
}