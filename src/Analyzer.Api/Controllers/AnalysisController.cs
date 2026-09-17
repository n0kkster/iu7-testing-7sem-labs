using Analyzer.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analyzer.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/analysis")]
public class AnalysisController(IAnalysisService analysisService) : ControllerBase
{
    private readonly IAnalysisService _analysisService = analysisService;

    [Authorize(Roles = "Architect, SRE")]
    [HttpGet("simulate/{failedComponentId:guid}")]
    public async Task<IActionResult> GetImpactedComponents(Guid failedComponentId)
    {
        var componentDtos = await _analysisService.GetImpactedComponentsAsync(failedComponentId);
        return Ok(componentDtos);
    }

    [Authorize(Roles = "Architect, SRE")]
    [HttpGet("cycles/{systemId:guid}")]
    public async Task<IActionResult> DetectCycles(Guid systemId)
    {
        var result = await _analysisService.DetectCyclesAsync(systemId);
        return Ok(result);
    }

    [Authorize(Roles = "Architect, SRE")]
    [HttpGet("spof/{systemId:guid}")]
    public async Task<IActionResult> DetectSpof(Guid systemId, [FromQuery] int threshold = 3)
    {
        var result = await _analysisService.DetectSpofAsync(systemId, threshold);
        return Ok(result);
    }

    [Authorize(Roles = "Architect, SRE")]
    [HttpGet("decommission/{targetComponentId:guid}")]
    public async Task<IActionResult> PlanDecommissioning(Guid targetComponentId)
    {
        var result = await _analysisService.PlanDecommissioningAsync(targetComponentId);
        return Ok(result);
    }

    [Authorize(Roles = "Developer")]
    [HttpGet("deployment-risk/{targetComponentId:guid}")]
    public async Task<IActionResult> AssessDeploymentRisk(Guid targetComponentId)
    {
        var result = await _analysisService.AssessDeploymentRiskAsync(targetComponentId);
        return Ok(result);
    }
}