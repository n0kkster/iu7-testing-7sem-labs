namespace Analyzer.Api.Controllers.V2;

using System.Security.Claims;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Shared.DTO.V2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/v2/analysis")]
public class AnalysisController(IAnalysisService analysisService) : ControllerBase
{
    private readonly IAnalysisService _analysisService = analysisService;

    [HttpPost]
    [ActionName("runAnalysis")]
    [ProducesResponseType(typeof(AnalysisResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunAnalysis([FromBody] AnalysisRequestDto request)
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (userRole == "Developer" && request.Type != AnalysisType.DeploymentRisk)
        {
            return Forbid();
        }

        var result = await _analysisService.ExecuteAnalysisAsync(request);
        return Ok(result);
    }
}