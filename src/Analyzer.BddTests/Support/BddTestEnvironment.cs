namespace Analyzer.BddTests.Support;

using Analyzer.Infrastructure.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Testcontainers.Neo4j;
using Testcontainers.PostgreSql;
using Npgsql;

public class BddTestEnvironment : IAsyncDisposable
{
    private readonly bool _useStand = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STAND_API_URL"));

    private PostgreSqlContainer? _pgContainer;
    private Neo4jContainer? _neo4jContainer;
    private IContainer? _mailpitContainer;
    private WebApplicationFactory<Program>? _factory;

    private string _pgConnectionString = string.Empty;

    public MailpitClient Mailpit { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (_useStand)
        {
            var mailpitUrl = Environment.GetEnvironmentVariable("STAND_MAILPIT_URL") ?? "http://localhost:8025";
            Mailpit = new MailpitClient(mailpitUrl);
            
            var baseStandConn = Environment.GetEnvironmentVariable("STAND_PG_CONNECTION")
                ?? "Host=localhost;Port=5439;Database=analyzer_stand_db;Username=postgres;Password=stand_password";
            _pgConnectionString = new NpgsqlConnectionStringBuilder(baseStandConn)
            {
                Database = "analyzer_stand_db"
            }.ConnectionString;

            return;
        }

        _pgContainer = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("analyzer_bdd_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _neo4jContainer = new Neo4jBuilder("neo4j:latest")
            .WithEnvironment("NEO4J_PLUGINS", "[\"apoc\"]")
            .WithEnvironment("NEO4J_dbms_security_procedures_unrestricted", "apoc.*")
            .Build();

        _mailpitContainer = new ContainerBuilder()
            .WithImage("axllent/mailpit:latest")
            .WithPortBinding(1025, true)
            .WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025).ForPath("/api/v1/messages")))
            .Build();

        await Task.WhenAll(
            _pgContainer.StartAsync(),
            _neo4jContainer.StartAsync(),
            _mailpitContainer.StartAsync()
        );

        var smtpHost = _mailpitContainer.Hostname;
        var smtpPort = _mailpitContainer.GetMappedPublicPort(1025);
        var mailpitApiUrl = $"http://{_mailpitContainer.Hostname}:{_mailpitContainer.GetMappedPublicPort(8025)}";
        Mailpit = new MailpitClient(mailpitApiUrl);

        var pgConn = _pgContainer.GetConnectionString();
        _pgConnectionString = pgConn;
        var neoUri = _neo4jContainer.GetConnectionString();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("DatabaseProvider", "Postgres");
            builder.UseSetting("ConnectionStrings:PGConnection", pgConn);
            builder.UseSetting("Neo4jSettings:Uri", neoUri);
            builder.UseSetting("Neo4jSettings:User", "neo4j");
            builder.UseSetting("Neo4jSettings:Password", "password");
            builder.UseSetting("SmtpSettings:Host", smtpHost);
            builder.UseSetting("SmtpSettings:Port", smtpPort.ToString());
            builder.UseSetting("SmtpSettings:From", "noreply@analyzer.local");
            builder.UseSetting("Jwt:Key", "SuperSecretTestingKeyForJwtSigning1234567890!");
            builder.UseSetting("Jwt:Issuer", "FaultAnalyzer");
            builder.UseSetting("Jwt:Audience", "FaultAnalyzerAudience");
            builder.UseSetting("Jwt:ExpireMinutes", "60");
            builder.UseSetting("AdminSettings:Username", SecretProvider.AdminUsername);
            builder.UseSetting("AdminSettings:Password", SecretProvider.AdminPassword);
            builder.UseSetting("AdminSettings:Email", "admin@analyzer.local");

            builder.ConfigureServices(services =>
            {
                var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AnalyzerDbContext>));
                if (dbDesc != null) services.Remove(dbDesc);

                services.AddDbContext<AnalyzerDbContext>(opt =>
                {
                    opt.UseNpgsql(pgConn);
                    opt.UseSnakeCaseNamingConvention();
                });

                var neoDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IDriver));
                if (neoDesc != null) services.Remove(neoDesc);

                services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(neoUri, AuthTokens.None));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                scope.ServiceProvider.GetRequiredService<AnalyzerDbContext>().Database.Migrate();
            });
        });
    }

    public async Task ExpireTwoFactorCodeAsync(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_pgConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "UPDATE users SET two_factor_code_expiry = NOW() - INTERVAL '1 minute' WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public HttpClient CreateApiClient()
    {
        if (_useStand)
            return new HttpClient { BaseAddress = new Uri(Environment.GetEnvironmentVariable("STAND_API_URL")!) };

        return _factory!.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory != null) await _factory.DisposeAsync();
        if (_pgContainer != null) await _pgContainer.DisposeAsync();
        if (_neo4jContainer != null) await _neo4jContainer.DisposeAsync();
        if (_mailpitContainer != null) await _mailpitContainer.DisposeAsync();
    }
}