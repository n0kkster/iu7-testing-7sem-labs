namespace Analyzer.Shared.DTO;

using Analyzer.Domain.Enums;

public class GraphPathDto 
{
    public List<Guid> NodeIds { get; set; } = [];
    public List<LinkSeverity> LinkSeverities { get; set; } = [];
}

public class CycleAnalysisResultDto
{
    public bool HasCycles => Cycles.Any();
    public List<List<Guid>> Cycles { get; set; } = [];
}

public class SpofAnalysisResultDto
{
    public bool HasSpof => CriticalNodes.Any();
    public Dictionary<Guid, int> CriticalNodes { get; set; } = [];
}

public class DecommissioningResultDto
{
    public bool IsSafeToDecommission => !ImpactedComponentIds.Any();
    public List<Guid> ImpactedComponentIds { get; set; } = [];
    public string Recommendation { get; set; } = string.Empty;
}

public class DeploymentRiskResultDto
{
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
    public int RiskScore { get; set; }
    public int TotalAffectedPaths { get; set; }
    public string Summary { get; set; } = string.Empty;
}