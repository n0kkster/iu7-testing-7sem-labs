namespace Analyzer.Application.Services;

using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Shared.DTO;
using Analyzer.Domain.Enums;

public class AnalysisService(IGraphRepository repository) : IAnalysisService
{
    readonly IGraphRepository _repository = repository;

    public async Task<IReadOnlyCollection<Guid>> GetImpactedComponentsAsync(Guid failedComponentId)
    {
        return await _repository.GetCascadingFailureImpactAsync(failedComponentId);
    }

    public async Task<CycleAnalysisResultDto> DetectCyclesAsync(Guid systemId)
    {
        var rawCycles = await _repository.GetCyclicDependenciesAsync(systemId);

        var result = new CycleAnalysisResultDto();
        
        foreach (var cycle in rawCycles)
            result.Cycles.Add(cycle.ToList());

        return result;
    }

    public async Task<SpofAnalysisResultDto> DetectSpofAsync(Guid systemId, int threshold = 3)
    {
        var spofNodes = await _repository.GetSinglePointsOfFailureAsync(systemId, threshold);

        return new SpofAnalysisResultDto
        {
            CriticalNodes = spofNodes.OrderByDescending(x => x.Value)
                                     .ToDictionary(x => x.Key, x => x.Value)
        };
    }

    public async Task<DecommissioningResultDto> PlanDecommissioningAsync(Guid targetComponentId)
    {
        var impactedIds = await _repository.GetDecommissioningImpactAsync(targetComponentId);

        var result = new DecommissioningResultDto
        {
            ImpactedComponentIds = impactedIds.ToList()
        };

        if (result.IsSafeToDecommission)
        {
            result.Recommendation = "Компонент можно безопасно отключить. От него не зависят другие узлы.";
        }
        else
        {
            result.Recommendation = $"ВНИМАНИЕ: Отключение приведет к сбою в {impactedIds.Count} связанных компонентах. " +
                                    "Необходимо перенастроить зависимости перед удалением.";
        }

        return result;
    }

    public async Task<DeploymentRiskResultDto> AssessDeploymentRiskAsync(Guid deployComponentId)
    {
        var paths = await _repository.GetDeploymentRiskPathsAsync(deployComponentId);

        var result = new DeploymentRiskResultDto
        {
            TotalAffectedPaths = paths.Count
        };

        if (paths.Count == 0)
        {
            result.RiskLevel = "Low";
            result.Summary = "Обновление безопасно. Никто не использует этот компонент.";
            return result;
        }

        int totalScore = 0;

        foreach (var path in paths)
        {
            int pathScore = 0;

            foreach (var severity in path.LinkSeverities)
            {
                pathScore += severity switch
                {
                    LinkSeverity.High => 10,  // Критичная связь дает большой риск
                    LinkSeverity.Mid => 3,    // Средняя связь дает умеренный риск
                    LinkSeverity.Low => 1,    // Низкая связь почти не дает риска
                    _ => 0
                };
            }
            totalScore += pathScore;
        }

        result.RiskScore = totalScore;

        if (totalScore >= 50 || paths.Any(p => p.LinkSeverities.Contains(LinkSeverity.High)))
        {
            result.RiskLevel = "Critical";
            result.Summary = "Критический риск! Развертывание вызовет простой зависимых критичных систем. " +
                             "Требуется согласование (Downtime window) или Blue/Green Deployment.";
        }
        else if (totalScore >= 20)
        {
            result.RiskLevel = "High";
            result.Summary = "Высокий риск. Возможно кратковременное снижение производительности или частичный отказ.";
        }
        else if (totalScore >= 5)
        {
            result.RiskLevel = "Medium";
            result.Summary = "Средний риск. Зависимые системы должны справиться благодаря механизмам Retry/Fallback.";
        }
        else
        {
            result.RiskLevel = "Low";
            result.Summary = "Низкий риск. Влияние на систему минимально.";
        }

        return result;
    }
}