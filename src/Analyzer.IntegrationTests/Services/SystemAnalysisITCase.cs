namespace Analyzer.IntegrationTests.Services;

using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.Infrastructure.Persistence;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Shared.DTO.Common;
using Analyzer.Shared.DTO.V2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection("Database collection")]
public class SystemAnalysisITCase : IClassFixture<SharedNeo4jFixture>, IDisposable
{
    private readonly IServiceScope _scope;
    private readonly ITeamRepository _teamRepository;
    private readonly ISystemService _systemService;
    private readonly IGraphService _graphService;
    private readonly IAnalysisService _analysisService;

    public SystemAnalysisITCase(SharedDatabaseFixture dbFixture, SharedNeo4jFixture neo4jFixture)
    {
        _scope = dbFixture.ServiceProvider.CreateScope();

        var systemRepository = _scope.ServiceProvider.GetRequiredService<ISystemRepository>();
        _teamRepository = _scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        var neoDriver = neo4jFixture.CreateDriver();
        var graphRepository = new Neo4jGraphRepository(neoDriver);

        _graphService = new GraphService(graphRepository);
        _analysisService = new AnalysisService(graphRepository);
        _systemService = new SystemService(_graphService, systemRepository);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Fact]
    public async Task ExecuteSystemTopologyAndFailureAnalysis_ShouldCoordinateAcrossSqlAndGraphDatabases()
    {
        // ====================================================================
        // ARRANGE
        // ====================================================================
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];

        var team = new Team($"Team_{uniqueSuffix}", "DevOps core");
        await _teamRepository.AddAsync(team);

        var systemId = await _systemService.CreateSystemAsync(new CreateITSystemDto
        {
            Name = $"System_{uniqueSuffix}",
            Description = "Microservice analysis test system",
            TeamId = team.Id
        });

        // ====================================================================
        // ACT & ASSERT: ШАГ 1 — Построение микросервисной топологии в Neo4j
        // ====================================================================
        // Создаем топологию: Frontend -> Gateway -> Database
        var frontendId = await _graphService.CreateComponentAsync(new CreateComponentDto(
            systemId, ComponentType.Microservice, "Frontend", "Client UI"));

        var gatewayId = await _graphService.CreateComponentAsync(new CreateComponentDto(
            systemId, ComponentType.Microservice, "API-Gateway", "Entry Gateway"));

        var databaseId = await _graphService.CreateComponentAsync(new CreateComponentDto(
            systemId, ComponentType.Database, "Primary-DB", "Data Storage"));

        await _graphService.CreateLinkAsync(new CreateLinkDto(
            frontendId, gatewayId, LinkSeverity.Mid, ProtocolType.REST));

        await _graphService.CreateLinkAsync(new CreateLinkDto(
            gatewayId, databaseId, LinkSeverity.High, ProtocolType.TCP));

        var components = await _graphService.GetComponentsBySystemIdAsync(systemId);
        components.Should().HaveCount(3);

        var links = await _graphService.GetLinksBySystemIdAsync(systemId);
        links.Should().HaveCount(2);

        // ====================================================================
        // ACT & ASSERT: ШАГ 2 — Каскадный сбой через ExecuteAnalysisAsync
        // ====================================================================
        var cascadeResponse = await _analysisService.ExecuteAnalysisAsync(new AnalysisRequestDto(
            Type: AnalysisType.CascadingFailure,
            ComponentId: databaseId
        ));

        cascadeResponse.Type.Should().Be(AnalysisType.CascadingFailure);
        cascadeResponse.ImpactedComponentIds.Should().NotBeNull();
        cascadeResponse.ImpactedComponentIds.Should().Contain(gatewayId);

        // ====================================================================
        // ACT & ASSERT: ШАГ 3 — Оценка рисков развертывания
        // ====================================================================
        var riskResponse = await _analysisService.ExecuteAnalysisAsync(new AnalysisRequestDto(
            Type: AnalysisType.DeploymentRisk,
            ComponentId: databaseId
        ));

        riskResponse.Type.Should().Be(AnalysisType.DeploymentRisk);
        riskResponse.DeploymentRisk.Should().NotBeNull();
        riskResponse.DeploymentRisk!.TotalAffectedPaths.Should().BeGreaterThan(0);

        // ====================================================================
        // ACT & ASSERT: ШАГ 4 — Поиск единых точек отказа (SPOF)
        // ====================================================================
        // База данных является точкой отказа для сервисов перед ней
        var spofResponse = await _analysisService.ExecuteAnalysisAsync(new AnalysisRequestDto(
            Type: AnalysisType.Spof,
            SystemId: systemId,
            Threshold: 1
        ));

        spofResponse.Type.Should().Be(AnalysisType.Spof);
        spofResponse.CriticalNodes.Should().NotBeNull();
        spofResponse.CriticalNodes.Should().ContainKey(databaseId);

        // ====================================================================
        // ACT & ASSERT: ШАГ 5 — Проверка циклов (До и после внесения)
        // ====================================================================
        var initialCycleResponse = await _analysisService.ExecuteAnalysisAsync(new AnalysisRequestDto(
            Type: AnalysisType.Cycles,
            SystemId: systemId
        ));
        initialCycleResponse.Cycles.Should().BeEmpty();

        // Замыкаем цикл: Database -> Frontend
        await _graphService.CreateLinkAsync(new CreateLinkDto(
            databaseId, frontendId, LinkSeverity.Low, ProtocolType.REST));

        var detectedCyclesResponse = await _analysisService.ExecuteAnalysisAsync(new AnalysisRequestDto(
            Type: AnalysisType.Cycles,
            SystemId: systemId
        ));

        detectedCyclesResponse.Cycles.Should().NotBeNull();
        detectedCyclesResponse.Cycles.Should().NotBeEmpty();
        detectedCyclesResponse.Cycles!.First().Should().Contain([frontendId, gatewayId, databaseId]);

        // ====================================================================
        // ACT & ASSERT: ШАГ 6 — Каскадное удаление (PostgreSQL + Neo4j)
        // ====================================================================
        await _systemService.DeleteSystemAsync(systemId);

        var remainingSystems = await _systemService.GetSystemsByTeamIdAsync(team.Id);
        remainingSystems.Should().NotContain(s => s.Id == systemId);

        var remainingComponents = await _graphService.GetComponentsBySystemIdAsync(systemId);
        remainingComponents.Should().BeEmpty();
    }
}