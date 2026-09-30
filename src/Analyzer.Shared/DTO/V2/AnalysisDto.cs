namespace Analyzer.Shared.DTO.V2;

using System.Text.Json.Serialization;
using Analyzer.Shared.DTO.V1;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisType
{
    CascadingFailure,
    Cycles,
    Spof,
    Decommissioning,
    DeploymentRisk
}

public record AnalysisRequestDto(
    AnalysisType Type,
    Guid? SystemId = null,
    Guid? ComponentId = null,
    int Threshold = 3
);

public record AnalysisResponseDto
{
    public required AnalysisType Type { get; init; }
    public IReadOnlyCollection<Guid>? ImpactedComponentIds { get; init; }
    public IReadOnlyCollection<IReadOnlyCollection<Guid>>? Cycles { get; init; }
    public Dictionary<Guid, int>? CriticalNodes { get; init; }
    public DeploymentRiskResultDto? DeploymentRisk { get; init; }
    public string? Recommendation { get; init; }
}