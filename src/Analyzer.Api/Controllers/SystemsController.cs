namespace Analyzer.Api.Controllers;

using System.Text;
using System.Text.Json;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v1/systems")]
public class SystemsController(ISystemService systemsService) : ControllerBase
{
    private readonly ISystemService _systemsService = systemsService;

    public record SystemStorage(
        IReadOnlyCollection<ComponentDto> Components,
        IReadOnlyCollection<LinkDto> Links);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ITSystemDto>>> ListSystemsByTeam([FromQuery] Guid teamId)
    {
        var systems = await _systemsService.GetSystemsByTeamIdAsync(teamId);
        return Ok(systems);
    }

    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetSystemById))]
    public async Task<ActionResult<ITSystemDto>> GetSystemById(Guid id)
    {
        var system = await _systemsService.GetSystemByIdAsync(id);
        return Ok(system);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSystem([FromBody] CreateITSystemDto dto)
    {
        var createdId = await _systemsService.CreateSystemAsync(dto);
        return CreatedAtAction(nameof(GetSystemById), new { id = createdId }, new { id = createdId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSystem(Guid id, [FromBody] UpdateITSystemDto dto)
    {
        await _systemsService.UpdateSystemAsync(id, dto);
        return NoContent();
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ITSystemDto>> PatchSystem(Guid id, [FromBody] PatchITSystemDto dto)
    {
        var updated = await _systemsService.PatchSystemAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSystem(Guid id)
    {
        await _systemsService.DeleteSystemAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> ExportSystem(Guid id)
    {
        var (components, links) = await _systemsService.ExportSystemAsync(id);
        var jsonString = JsonSerializer.Serialize(new SystemStorage(components, links));
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var fileName = $"system-backup-{DateTime.UtcNow:yyyy-MM-dd_HH-mm}.json";

        return File(bytes, "application/json", fileName);
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportSystem(IFormFile file, [FromForm] string importData)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Файл импорта не предоставлен.");

        var dto = JsonSerializer.Deserialize<CreateITSystemDto>(
            importData,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (dto is null)
            return BadRequest("Некорректные параметры новой системы.");

        using var stream = file.OpenReadStream();
        var storage = await JsonSerializer.DeserializeAsync<SystemStorage>(stream);

        if (storage is null)
            return BadRequest("Ошибка десериализации топологии системы.");

        var newSystemId = await _systemsService.ImportSystemAsync(storage.Components, storage.Links, dto);
        return CreatedAtAction(nameof(GetSystemById), new { id = newSystemId }, new { id = newSystemId });
    }
}