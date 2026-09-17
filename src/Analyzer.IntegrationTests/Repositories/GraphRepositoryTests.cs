using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Infrastructure.Persistence;
using FluentAssertions;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Neo4j collection")]
public class GraphRepositoryTests(SharedNeo4jFixture fixture)
{
    [Fact]
    public async Task AddComponentAsync_ShouldSaveNodeToGraph()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);

        var systemId = Guid.NewGuid();
        var component = new Component
        {
            SystemId = systemId,
            Name = "Auth Microservice",
            Description = "Handles user authentication",
            Type = ComponentType.Microservice
        };

        // Act
        await repository.AddComponentAsync(component);

        // Assert
        var result = await repository.GetComponentAsync(component.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Auth Microservice");
        result.Type.Should().Be(ComponentType.Microservice);
        result.SystemId.Should().Be(systemId);
    }

    [Fact]
    public async Task AddLinkAsync_ShouldCreateRelationshipBetweenComponents()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var source = new Component
        {
            SystemId = systemId,
            Name = "Frontend",
            Description = "UI",
            Type = ComponentType.Unknown
        };

        var target = new Component
        {
            SystemId = systemId,
            Name = "Backend API",
            Description = "API",
            Type = ComponentType.Microservice
        };

        await repository.AddComponentAsync(source);
        await repository.AddComponentAsync(target);

        var link = new Link
        {
            SourceId = source.Id,
            TargetId = target.Id,
            Severity = LinkSeverity.High,
            Protocol = ProtocolType.REST
        };

        // Act
        await repository.AddLinkAsync(link);

        // Assert
        var links = await repository.GetLinksBySystemIdAsync(systemId);

        links.Should().HaveCount(1);
        links.First().SourceId.Should().Be(source.Id);
        links.First().TargetId.Should().Be(target.Id);
        links.First().Protocol.Should().Be(ProtocolType.REST);
        links.First().Severity.Should().Be(LinkSeverity.High);
    }
    [Fact]
    public async Task GetComponentsBySystemIdAsync_ShouldReturnOnlySystemsComponents()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);

        var targetSystemId = Guid.NewGuid();
        var otherSystemId = Guid.NewGuid();

        // Целевая система
        await repository.AddComponentAsync(
            new Component
            {
                SystemId = targetSystemId,
                Name = "C1",
                Description = "D1",
                Type = ComponentType.Database
            }
        );

        await repository.AddComponentAsync(
            new Component
            {
                SystemId = targetSystemId,
                Name = "C2",
                Description = "D2",
                Type = ComponentType.Database
            }
        );

        // Другая система
        await repository.AddComponentAsync(
            new Component
            {
                SystemId = otherSystemId,
                Name = "Noise",
                Description = "Noise",
                Type = ComponentType.Unknown
            }
        );

        // Act
        var components = await repository.GetComponentsBySystemIdAsync(targetSystemId);

        // Assert
        components.Should().HaveCount(2);
        components.All(c => c.SystemId == targetSystemId).Should().BeTrue();
    }

    [Fact]
    public async Task GetCascadingFailureImpactAsync_ShouldReturnDependentNodes()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var compA = new Component
        {
            SystemId = systemId,
            Name = "Service A",
            Description = "A",
            Type = ComponentType.Microservice
        };

        var compB = new Component
        {
            SystemId = systemId,
            Name = "Service B",
            Description = "B",
            Type = ComponentType.Microservice
        };

        var compC = new Component
        {
            SystemId = systemId,
            Name = "Database C",
            Description = "C",
            Type = ComponentType.Database
        };

        await repository.AddComponentAsync(compA);
        await repository.AddComponentAsync(compB);
        await repository.AddComponentAsync(compC);

        await repository.AddLinkAsync(
            new Link
            {
                SourceId = compA.Id,
                TargetId = compB.Id,
                Protocol = ProtocolType.REST,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link
            {
                SourceId = compB.Id,
                TargetId = compC.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        // Act
        var impactedIds = await repository.GetCascadingFailureImpactAsync(compC.Id);

        // Assert
        // compA и compB должны пострадать
        impactedIds.Should().Contain(compB.Id);
    }


    [Fact]
    public async Task GetCyclicDependenciesAsync_ShouldReturnCycles()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var compA = new Component 
        { 
            SystemId = systemId, 
            Name = "Service A", 
            Description = "A", 
            Type = ComponentType.Microservice 
        };
        
        var compB = new Component 
        { 
            SystemId = systemId, 
            Name = "Service B", 
            Description = "B", 
            Type = ComponentType.Microservice     
        };

        var compC = new Component 
        { 
            SystemId = systemId, 
            Name = "Database C", 
            Description = "C", 
            Type = ComponentType.Database 
        };

        await repository.AddComponentAsync(compA);
        await repository.AddComponentAsync(compB);
        await repository.AddComponentAsync(compC);

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compA.Id, 
                TargetId = compB.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compB.Id, 
                TargetId = compC.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );
        
        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compC.Id, 
                TargetId = compA.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        // Act
        var cycles = await repository.GetCyclicDependenciesAsync(systemId);

        // Assert
        cycles.Should().NotBeEmpty();
        cycles.Should().HaveCount(1);

        var cycleNodes = cycles.First();
        cycleNodes.Should().Contain(compA.Id);
        cycleNodes.Should().Contain(compB.Id);
        cycleNodes.Should().Contain(compC.Id);
    }

    [Fact]
    public async Task GetSinglePointsOfFailureAsync_ShouldReturnHighlyDependedNodes()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var compA = new Component 
        { 
            SystemId = systemId, 
            Name = "Serice A", 
            Description = "A", 
            Type = ComponentType.Microservice 
        };

        var compB = new Component 
        { 
            SystemId = systemId, 
            Name = "Service B", 
            Description = "B", 
            Type = ComponentType.Microservice 
        };

        var compC = new Component 
        { 
            SystemId = systemId, 
            Name = "Service C", 
            Description = "C", 
            Type = ComponentType.Microservice 
        };

        var authDb = new Component 
        { 
            SystemId = systemId, 
            Name = "AuthDB", 
            Description = "Authentication database", 
            Type = ComponentType.Database 
        };

        var coreDb = new Component 
        { 
            SystemId = systemId, 
            Name = "CoreDB", 
            Description = "Core database", 
            Type = ComponentType.Database 
        };

        foreach (var c in new[] { compA, compB, compC, authDb, coreDb })
            await repository.AddComponentAsync(c);

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compA.Id, 
                TargetId = authDb.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compB.Id, 
                TargetId = authDb.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compC.Id, 
                TargetId = authDb.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = authDb.Id, 
                TargetId = coreDb.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );


        var spofs = await repository.GetSinglePointsOfFailureAsync(systemId, threshold: 3);

        // Assert
        spofs.Should().ContainKey(authDb.Id)
             .WhoseValue.Should().Be(3);

        spofs.Should().ContainKey(coreDb.Id)
             .WhoseValue.Should().Be(4);

        spofs.Should().NotContainKey(compA.Id);
    }

    [Fact]
    public async Task GetDecommissioningImpactAsync_ShouldReturnTransitiveDependents()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var compA = new Component 
        { 
            SystemId = systemId, 
            Name = "Service A", 
            Description = "A", 
            Type = ComponentType.Microservice 
        };

        var compB = new Component 
        { 
            SystemId = systemId, 
            Name = "Service B", 
            Description = "B", 
            Type = ComponentType.Microservice 
        };

        var compC = new Component 
        { 
            SystemId = systemId, 
            Name = "Database C", 
            Description = "C", 
            Type = ComponentType.Database 
        };

        var compD = new Component 
        { 
            SystemId = systemId, 
            Name = "Service D", 
            Description= "D", 
            Type = ComponentType.Microservice 
        };


        foreach (var c in new[] { compA, compB, compC, compD })
            await repository.AddComponentAsync(c);

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compA.Id, 
                TargetId = compB.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compB.Id, 
                TargetId = compC.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compD.Id, 
                TargetId = compC.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );


        // Act
        var impactedIds = await repository.GetDecommissioningImpactAsync(compC.Id);

        // Assert
        impactedIds.Should().HaveCount(3);
        impactedIds.Should().Contain(compA.Id);
        impactedIds.Should().Contain(compB.Id);
        impactedIds.Should().Contain(compD.Id);
    }

    [Fact]
    public async Task GetDeploymentRiskPathsAsync_ShouldReturnAllPathsToTarget()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var compA = new Component 
        { 
            SystemId = systemId, 
            Name = "Service A", 
            Description = "A", 
            Type = ComponentType.Microservice 
        };

        var compB = new Component 
        { 
            SystemId = systemId, 
            Name = "Service B", 
            Description = "B", 
            Type = ComponentType.Microservice 
        };

        var compC = new Component 
        { 
            SystemId = systemId, 
            Name = "Service C", 
            Description = "C", 
            Type = ComponentType.Database 
        };

        foreach (var c in new[] { compA, compB, compC })
            await repository.AddComponentAsync(c);

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compA.Id, 
                TargetId = compB.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compB.Id, 
                TargetId = compC.Id,
                Protocol = ProtocolType.TCP,
                Severity = LinkSeverity.High
            }
        );

        // Act
        var riskPaths = await repository.GetDeploymentRiskPathsAsync(compC.Id);

        // Assert
        riskPaths.Should().HaveCount(2);
        riskPaths.Should().Contain(p => p.NodeIds.SequenceEqual(new[] { compA.Id, compB.Id, compC.Id }));        
        riskPaths.Should().Contain(p => p.NodeIds.SequenceEqual(new[] {compB.Id, compC.Id }));
    }
}