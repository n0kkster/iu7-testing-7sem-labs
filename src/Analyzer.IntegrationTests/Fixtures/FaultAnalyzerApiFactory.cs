namespace Analyzer.IntegrationTests.Fixtures;

using Analyzer.Infrastructure.Data;
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
    private readonly bool _useStand = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STAND_API_URL"));

    private PostgreSqlContainer? _pgContainer;
    private Neo4jContainer? _neo4jContainer;

    public async Task InitializeAsync()
    {
        if (_useStand) return;

        _pgContainer = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("analyzer_e2e_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _neo4jContainer = new Neo4jBuilder("neo4j:latest")
            .WithEnvironment("NEO4J_PLUGINS", "[\"apoc\"]")
            .WithEnvironment("NEO4J_apoc_export_file_enabled", "true")
            .WithEnvironment("NEO4J_apoc_import_file_enabled", "true")
            .WithEnvironment("NEO4J_dbms_security_procedures_unrestricted", "apoc.*")
            .Build();

        await Task.WhenAll(_pgContainer.StartAsync(), _neo4jContainer.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        if (_useStand) return;

        if (_pgContainer != null) await _pgContainer.DisposeAsync();
        if (_neo4jContainer != null) await _neo4jContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_useStand) return;

        var pgConn = _pgContainer!.GetConnectionString();
        var neoUri = _neo4jContainer!.GetConnectionString();

        builder.UseEnvironment("Testing");
        builder.UseSetting("DatabaseProvider", "Postgres");
        builder.UseSetting("ConnectionStrings:PGConnection", pgConn);
        builder.UseSetting("Neo4jSettings:Uri", neoUri);
        builder.UseSetting("Neo4jSettings:User", "neo4j");
        builder.UseSetting("Neo4jSettings:Password", "password");

        builder.UseSetting("Jwt:Key", "SuperSecretTestingKeyForJwtSigning1234567890!");
        builder.UseSetting("Jwt:Issuer", "FaultAnalyzer");
        builder.UseSetting("Jwt:Audience", "FaultAnalyzerAudience");
        builder.UseSetting("Jwt:ExpireMinutes", "60");
        builder.UseSetting("AdminSettings:Username", "admin");
        builder.UseSetting("AdminSettings:Password", "AdminSecret123!");
        builder.UseSetting("AdminSettings:Email", "admin@analyzer.local");

        builder.ConfigureServices(services =>
        {
            var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AnalyzerDbContext>));
            if (dbDesc != null) services.Remove(dbDesc);

            services.AddDbContext<AnalyzerDbContext>(options =>
            {
                options.UseNpgsql(pgConn);
                options.UseSnakeCaseNamingConvention();
            });

            var neoDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IDriver));
            if (neoDesc != null) services.Remove(neoDesc);

            services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(neoUri, AuthTokens.None));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<AnalyzerDbContext>().Database.Migrate();
        });
    }
}