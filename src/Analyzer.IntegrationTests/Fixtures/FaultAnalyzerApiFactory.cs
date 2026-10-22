namespace Analyzer.IntegrationTests.Fixtures;

using Analyzer.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Npgsql;
using Xunit;

public class FaultAnalyzerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TemplateDbName = "analyzer_template";
    private readonly string _testDbName = $"analyzer_e2e_{Guid.NewGuid().ToString("N")[..8]}";
    
    private readonly bool _useStand = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STAND_API_URL"));

    private readonly string _masterConnectionString = Environment.GetEnvironmentVariable("STAND_PG_CONNECTION") 
        ?? "Host=localhost;Port=5439;Database=postgres;Username=postgres;Password=stand_password";

    private string _testDbConnectionString = "";

    public async Task InitializeAsync()
    {
        if (_useStand) return;

        _testDbConnectionString = new NpgsqlConnectionStringBuilder(_masterConnectionString)
        {
            Database = _testDbName
        }.ConnectionString;

        await SetupTemplateAndCloneDatabaseAsync();
    }

    private async Task SetupTemplateAndCloneDatabaseAsync()
    {
        await using var masterConn = new NpgsqlConnection(_masterConnectionString);
        await masterConn.OpenAsync();

        // 1. Проверяем наличие шаблона
        await using var checkCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @db", masterConn);
        checkCmd.Parameters.AddWithValue("db", TemplateDbName);
        var templateExists = await checkCmd.ExecuteScalarAsync() != null;

        if (!templateExists)
        {
            await using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_lock(1338);", masterConn);
            await lockCmd.ExecuteNonQueryAsync();

            try
            {
                templateExists = await checkCmd.ExecuteScalarAsync() != null;
                if (!templateExists)
                {
                    await using var createCmd = new NpgsqlCommand($"CREATE DATABASE {TemplateDbName};", masterConn);
                    await createCmd.ExecuteNonQueryAsync();

                    var templateConnStr = new NpgsqlConnectionStringBuilder(_masterConnectionString) { Database = TemplateDbName }.ConnectionString;
                    var options = new DbContextOptionsBuilder<AnalyzerDbContext>()
                        .UseNpgsql(templateConnStr).UseSnakeCaseNamingConvention().Options;
                    await using var ctx = new AnalyzerDbContext(options);
                    await ctx.Database.MigrateAsync();

                    await using var killCmd = new NpgsqlCommand($@"
                        SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{TemplateDbName}';", masterConn);
                    await killCmd.ExecuteNonQueryAsync();

                    await using var alterCmd = new NpgsqlCommand($@"
                        ALTER DATABASE {TemplateDbName} ALLOW_CONNECTIONS = false;
                        UPDATE pg_database SET datistemplate = true WHERE datname = '{TemplateDbName}';", masterConn);
                    await alterCmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(1338);", masterConn);
                await unlockCmd.ExecuteNonQueryAsync();
            }
        }

        await using var cloneCmd = new NpgsqlCommand($"CREATE DATABASE {_testDbName} TEMPLATE {TemplateDbName};", masterConn);
        await cloneCmd.ExecuteNonQueryAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_useStand) return;

        await using var masterConn = new NpgsqlConnection(_masterConnectionString);
        await masterConn.OpenAsync();

        await using var killCmd = new NpgsqlCommand($@"
            SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_testDbName}';", masterConn);
        await killCmd.ExecuteNonQueryAsync();

        await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {_testDbName};", masterConn);
        await dropCmd.ExecuteNonQueryAsync();

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_useStand) return;

        builder.UseEnvironment("Testing");
        builder.UseSetting("DatabaseProvider", "Postgres");
        builder.UseSetting("ConnectionStrings:PGConnection", _testDbConnectionString);
        
        builder.UseSetting("Neo4jSettings:Uri", Environment.GetEnvironmentVariable("STAND_NEO4J_URI") ?? "bolt://localhost:7689");
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
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AnalyzerDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

            services.AddDbContext<AnalyzerDbContext>(options =>
            {
                options.UseNpgsql(_testDbConnectionString);
                options.UseSnakeCaseNamingConvention();
            });

            var neoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDriver));
            if (neoDescriptor != null) services.Remove(neoDescriptor);

            services.AddSingleton<IDriver>(sp => GraphDatabase.Driver(
                Environment.GetEnvironmentVariable("STAND_NEO4J_URI") ?? "bolt://localhost:7689", AuthTokens.None));
        });
    }
}