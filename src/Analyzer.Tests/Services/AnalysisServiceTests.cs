using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Services;
using Analyzer.Domain.Enums;
using Analyzer.Shared.DTO;
using Moq;

namespace Analyzer.Tests.Services;

public class AnalysisServiceTests
{
    private readonly Mock<IGraphRepository> _graphRepoMock;
    private readonly AnalysisService _analysisService;

    public AnalysisServiceTests()
    {
        _graphRepoMock = new Mock<IGraphRepository>();
        _analysisService = new AnalysisService(_graphRepoMock.Object);
    }

    #region 1. GetImpactedComponentsAsync

    [Fact]
    public async Task GetImpactedComponentsAsync_HasImpact_ReturnsGuidList()
    {
        // Arrange
        var failedComponentId = Guid.NewGuid();
        var impactedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _graphRepoMock.Setup(r => r.GetCascadingFailureImpactAsync(failedComponentId))
            .ReturnsAsync(impactedIds);

        // Act
        var result = await _analysisService.GetImpactedComponentsAsync(failedComponentId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equivalent(impactedIds, result);
    }

    [Fact]
    public async Task GetImpactedComponentsAsync_NoImpact_ReturnsEmptyList()
    {
        // Arrange
        var failedComponentId = Guid.NewGuid();
        _graphRepoMock.Setup(r => r.GetCascadingFailureImpactAsync(failedComponentId))
            .ReturnsAsync([]);

        // Act
        var result = await _analysisService.GetImpactedComponentsAsync(failedComponentId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region 2. DetectCyclesAsync

    [Fact]
    public async Task DetectCyclesAsync_NoCycles_ReturnsHasCyclesFalse()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        _graphRepoMock.Setup(r => r.GetCyclicDependenciesAsync(systemId))
            .ReturnsAsync([]);

        // Act
        var result = await _analysisService.DetectCyclesAsync(systemId);

        // Assert
        Assert.False(result.HasCycles);
        Assert.Empty(result.Cycles);
    }

    [Fact]
    public async Task DetectCyclesAsync_HasMultipleCycles_ReturnsMappedCycles()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var cycle1 = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var cycle2 = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        IReadOnlyCollection<IReadOnlyCollection<Guid>> mockRepoResult = [cycle1, cycle2];

        _graphRepoMock.Setup(r => r.GetCyclicDependenciesAsync(systemId))
            .ReturnsAsync(mockRepoResult);

        // Act
        var result = await _analysisService.DetectCyclesAsync(systemId);

        // Assert
        Assert.True(result.HasCycles);
        Assert.Equal(2, result.Cycles.Count);
        Assert.Equal(cycle2.Count, result.Cycles[1].Count);
    }

    #endregion

    #region 3. DetectSpofAsync

    [Fact]
    public async Task DetectSpofAsync_NoSpofFound_ReturnsEmptyDictionary()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        _graphRepoMock.Setup(r => r.GetSinglePointsOfFailureAsync(systemId, It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var result = await _analysisService.DetectSpofAsync(systemId);

        // Assert
        Assert.False(result.HasSpof);
        Assert.Empty(result.CriticalNodes);
    }

    [Fact]
    public async Task DetectSpofAsync_FoundSpofs_ReturnsSortedDictionary()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var nodeLowImpact = Guid.NewGuid();
        var nodeHighImpact = Guid.NewGuid();
        var nodeMidImpact = Guid.NewGuid();

        var unsortedSpofs = new Dictionary<Guid, int>
        {
            { nodeLowImpact, 4 },
            { nodeHighImpact, 15 },
            { nodeMidImpact, 8 }
        };

        _graphRepoMock.Setup(r => r.GetSinglePointsOfFailureAsync(systemId, 3))
            .ReturnsAsync(unsortedSpofs);

        // Act
        var result = await _analysisService.DetectSpofAsync(systemId);

        // Assert
        Assert.True(result.HasSpof);
        Assert.Equal(3, result.CriticalNodes.Count);

        var orderedKeys = result.CriticalNodes.Keys.ToList();
        Assert.Equal(nodeHighImpact, orderedKeys[0]);
        Assert.Equal(nodeMidImpact, orderedKeys[1]);
        Assert.Equal(nodeLowImpact, orderedKeys[2]);
    }

    #endregion

    #region 4. PlanDecommissioningAsync

    [Fact]
    public async Task PlanDecommissioningAsync_NoImpact_ReturnsSafeRecommendation()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        _graphRepoMock.Setup(r => r.GetDecommissioningImpactAsync(componentId))
            .ReturnsAsync([]);

        // Act
        var result = await _analysisService.PlanDecommissioningAsync(componentId);

        // Assert
        Assert.True(result.IsSafeToDecommission);
        Assert.Empty(result.ImpactedComponentIds);
        Assert.Contains("безопасно отключить", result.Recommendation);
    }

    [Fact]
    public async Task PlanDecommissioningAsync_WithImpact_ReturnsWarningRecommendation()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var impactedList = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        _graphRepoMock.Setup(r => r.GetDecommissioningImpactAsync(componentId))
            .ReturnsAsync(impactedList);

        // Act
        var result = await _analysisService.PlanDecommissioningAsync(componentId);

        // Assert
        Assert.False(result.IsSafeToDecommission);
        Assert.Equal(3, result.ImpactedComponentIds.Count);
        Assert.Contains("ВНИМАНИЕ: Отключение приведет к сбою в 3 связанных компонентах", result.Recommendation);
    }

    #endregion

    #region 5. AssessDeploymentRiskAsync
    [Fact]
    public async Task AssessDeploymentRiskAsync_NoPaths_ReturnsLowRiskAndZeroScore()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        _graphRepoMock.Setup(r => r.GetDeploymentRiskPathsAsync(componentId))
            .ReturnsAsync([]);

        // Act
        var result = await _analysisService.AssessDeploymentRiskAsync(componentId);

        // Assert
        Assert.Equal(0, result.TotalAffectedPaths);
        Assert.Equal(0, result.RiskScore);
        Assert.Equal("Low", result.RiskLevel);
        Assert.Contains("Никто не использует этот компонент", result.Summary);
    }

    [Fact]
    public async Task AssessDeploymentRiskAsync_HasHighSeverityLink_ReturnsCriticalRisk()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var paths = new List<GraphPathDto>
        {
            new() { LinkSeverities = [LinkSeverity.High, LinkSeverity.Low] }
        };
        _graphRepoMock.Setup(r => r.GetDeploymentRiskPathsAsync(componentId)).ReturnsAsync(paths);

        // Act
        var result = await _analysisService.AssessDeploymentRiskAsync(componentId);

        // Assert
        Assert.Equal(11, result.RiskScore);
        Assert.Equal("Critical", result.RiskLevel);
        Assert.Contains("Критический риск!", result.Summary);
    }

    [Fact]
    public async Task AssessDeploymentRiskAsync_Score50OrMore_ReturnsCriticalRisk()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var severities = Enumerable.Repeat(LinkSeverity.Mid, 17).ToList();

        var paths = new List<GraphPathDto> { new() { LinkSeverities = severities } };
        _graphRepoMock.Setup(r => r.GetDeploymentRiskPathsAsync(componentId)).ReturnsAsync(paths);

        // Act
        var result = await _analysisService.AssessDeploymentRiskAsync(componentId);

        // Assert
        Assert.Equal(51, result.RiskScore);
        Assert.Equal("Critical", result.RiskLevel);
    }

    [Fact]
    public async Task AssessDeploymentRiskAsync_ScoreBetween20And49_ReturnsHighRisk()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var severities = Enumerable.Repeat(LinkSeverity.Mid, 7).ToList();

        var paths = new List<GraphPathDto> { new() { LinkSeverities = severities } };
        _graphRepoMock.Setup(r => r.GetDeploymentRiskPathsAsync(componentId)).ReturnsAsync(paths);

        // Act
        var result = await _analysisService.AssessDeploymentRiskAsync(componentId);

        // Assert
        Assert.Equal(21, result.RiskScore);
        Assert.Equal("High", result.RiskLevel);
        Assert.Contains("Высокий риск", result.Summary);
    }

    [Fact]
    public async Task AssessDeploymentRiskAsync_ScoreBetween5And19_ReturnsMediumRisk()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var paths = new List<GraphPathDto>
        {
            new() { LinkSeverities = [LinkSeverity.Mid, LinkSeverity.Mid] }
        };
        _graphRepoMock.Setup(r => r.GetDeploymentRiskPathsAsync(componentId)).ReturnsAsync(paths);

        // Act
        var result = await _analysisService.AssessDeploymentRiskAsync(componentId);

        // Assert
        Assert.Equal(6, result.RiskScore);
        Assert.Equal("Medium", result.RiskLevel);
        Assert.Contains("Средний риск", result.Summary);
    }

    [Fact]
    public async Task AssessDeploymentRiskAsync_ScoreBetween1And4_ReturnsLowRisk()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var paths = new List<GraphPathDto>
        {
            new() { LinkSeverities =[LinkSeverity.Low, LinkSeverity.Low] }
        };
        _graphRepoMock.Setup(r => r.GetDeploymentRiskPathsAsync(componentId)).ReturnsAsync(paths);

        // Act
        var result = await _analysisService.AssessDeploymentRiskAsync(componentId);

        // Assert
        Assert.Equal(2, result.RiskScore);
        Assert.Equal("Low", result.RiskLevel);
        Assert.Contains("Низкий риск", result.Summary);
    }

    [Fact]
    public async Task AssessDeploymentRiskAsync_IgnoresUnknownSeverity_CalculatesProperly()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var paths = new List<GraphPathDto>
        {
            new() { LinkSeverities = [LinkSeverity.Unknown, LinkSeverity.Mid] }
        };
        _graphRepoMock.Setup(r => r.GetDeploymentRiskPathsAsync(componentId)).ReturnsAsync(paths);

        // Act
        var result = await _analysisService.AssessDeploymentRiskAsync(componentId);

        // Assert
        Assert.Equal(3, result.RiskScore);
        Assert.Equal("Low", result.RiskLevel);
    }

    #endregion
}