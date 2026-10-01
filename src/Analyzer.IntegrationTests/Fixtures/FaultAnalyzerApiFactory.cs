namespace Analyzer.IntegrationTests.Fixtures;

using Analyzer.Infrastructure.Data;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Testcontainers.Neo4j;
using Testcontainers.PostgreSql;
using Xunit;

public class FaultAnalyzerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("analyzer_e2e_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly Neo4jContainer _neo4jContainer = new Neo4jBuilder("neo4j:latest")
        .WithEnvironment("NEO4J_PLUGINS", "[\"apoc\"]")
        .WithEnvironment("NEO4J_apoc_export_file_enabled", "true")
        .WithEnvironment("NEO4J_apoc_import_file_enabled", "true")
        .WithEnvironment("NEO4J_dbms_security_procedures_unrestricted", "apoc.*")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_pgContainer.StartAsync(), _neo4jContainer.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(_pgContainer.DisposeAsync().AsTask(), _neo4jContainer.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("DatabaseProvider", "Postgres");
        builder.UseSetting("ConnectionStrings:PGConnection", _pgContainer.GetConnectionString());
        builder.UseSetting("Neo4jSettings:Uri", _neo4jContainer.GetConnectionString());
        builder.UseSetting("Neo4jSettings:User", "neo4j");
        builder.UseSetting("Neo4jSettings:Password", "password");
        builder.UseSetting("AdminSettings:Username", "admin");
        builder.UseSetting("AdminSettings:Password", "AdminSecret123!");
        builder.UseSetting("AdminSettings:Email", "admin@analyzer.local");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AnalyzerDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<AnalyzerDbContext>(options =>
            {
                options.UseNpgsql(_pgContainer.GetConnectionString());
                options.UseSnakeCaseNamingConvention();
            });

            var neoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDriver));
            if (neoDescriptor != null)
                services.Remove(neoDescriptor);

            services.AddSingleton(sp =>
                GraphDatabase.Driver(_neo4jContainer.GetConnectionString(), AuthTokens.None));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AnalyzerDbContext>();
            db.Database.Migrate();
        });
    }
}