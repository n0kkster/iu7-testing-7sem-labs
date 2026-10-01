using Neo4j.Driver;
using Testcontainers.Neo4j;

namespace Analyzer.IntegrationTests.Fixtures;

public class SharedNeo4jFixture : IAsyncLifetime
{
    private readonly Neo4jContainer _neo4jContainer = new Neo4jBuilder("neo4j:latest")
        .WithEnvironment("NEO4J_PLUGINS", "[\"apoc\"]")
        .WithEnvironment("NEO4J_apoc_export_file_enabled", "true")
        .WithEnvironment("NEO4J_apoc_import_file_enabled", "true")
        .WithEnvironment("NEO4J_dbms_security_procedures_unrestricted", "apoc.*")
        .Build();

    public async Task InitializeAsync()
    {
        await _neo4jContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _neo4jContainer.DisposeAsync();
    }

    public IDriver CreateDriver()
    {
        return GraphDatabase.Driver(_neo4jContainer.GetConnectionString(), AuthTokens.None);
    }
}

[CollectionDefinition("Neo4j collection")]
public class Neo4jCollection : ICollectionFixture<SharedNeo4jFixture>
{ }