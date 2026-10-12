using Neo4j.Driver;
using Testcontainers.Neo4j;

namespace Analyzer.IntegrationTests.Fixtures;

public class SharedNeo4jFixture : IAsyncLifetime
{
    private readonly bool _useStand = Environment.GetEnvironmentVariable("USE_STAND") == "true";
    private Neo4jContainer? _neo4jContainer;

    public async Task InitializeAsync()
    {
        if (!_useStand)
        {
            _neo4jContainer = new Neo4jBuilder("neo4j:latest")
                .WithEnvironment("NEO4J_PLUGINS", "[\"apoc\"]")
                .WithEnvironment("NEO4J_apoc_export_file_enabled", "true")
                .WithEnvironment("NEO4J_apoc_import_file_enabled", "true")
                .WithEnvironment("NEO4J_dbms_security_procedures_unrestricted", "apoc.*")
                .Build();

            await _neo4jContainer.StartAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_neo4jContainer != null)
            await _neo4jContainer.DisposeAsync();
    }

    public IDriver CreateDriver()
    {
        if (_useStand)
        {
            var uri = Environment.GetEnvironmentVariable("STAND_NEO4J_URI") ?? "bolt://localhost:7689";
            return GraphDatabase.Driver(uri, AuthTokens.None);
        }

        return GraphDatabase.Driver(_neo4jContainer!.GetConnectionString(), AuthTokens.None);
    }

    public async Task WipeSystemDataAsync(Guid systemId)
    {
        var driver = CreateDriver();
        await using var session = driver.AsyncSession();
        
        await session.RunAsync(
            "MATCH (n) WHERE n.system_id = $SysId DETACH DELETE n",
            new { SysId = systemId.ToString() });
    }
}

[CollectionDefinition("Neo4j collection")]
public class Neo4jCollection : ICollectionFixture<SharedNeo4jFixture>
{ }