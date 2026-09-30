using Analyzer.Shared.DTO.V1;
using Analyzer.Shared.DTO.V2;

namespace Analyzer.Application.Interfaces.Services;

public interface IAnalysisService
{
    Task<IReadOnlyCollection<Guid>> GetImpactedComponentsAsync(Guid failedComponentId);
    Task<CycleAnalysisResultDto> DetectCyclesAsync(Guid systemId);
    Task<SpofAnalysisResultDto> DetectSpofAsync(Guid systemId, int threshold = 3);
    Task<DecommissioningResultDto> PlanDecommissioningAsync(Guid targetComponentId);
    Task<DeploymentRiskResultDto> AssessDeploymentRiskAsync(Guid deployComponentId);

    Task<AnalysisResponseDto> ExecuteAnalysisAsync(AnalysisRequestDto request);
}