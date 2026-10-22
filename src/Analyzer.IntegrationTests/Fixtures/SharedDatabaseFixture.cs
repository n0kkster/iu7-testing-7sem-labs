namespace Analyzer.IntegrationTests.Fixtures;

using System.Data.Common;
using Analyzer.Application.Interfaces.Providers;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Infrastructure.Data;
using Analyzer.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

using Postgres = Infrastructure.Persistence.Postgres;

public class SharedDatabaseFixture : IAsyncLifetime
{
    private const string TemplateDbName = "analyzer_template";
    private readonly string _testDbName = $"analyzer_test_{Guid.NewGuid().ToString("N")[..8]}";
    
    // Базовая строка подключения к серверу стенда (по умолчанию к базе postgres)
    private readonly string _masterConnectionString = Environment.GetEnvironmentVariable("STAND_PG_CONNECTION") 
        ?? "Host=localhost;Port=5439;Database=postgres;Username=postgres;Password=stand_password";

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await SetupTemplateAndCloneDatabaseAsync();

        // Формируем строку подключения к уникальной изолированной базе этого прогона
        var testDbConnectionString = new NpgsqlConnectionStringBuilder(_masterConnectionString)
        {
            Database = _testDbName
        }.ConnectionString;

        var services = new ServiceCollection();

        services.AddDbContext<AnalyzerDbContext>(options =>
        {
            options.UseNpgsql(testDbConnectionString);
            options.UseSnakeCaseNamingConvention();
        });

        // Репозитории и сервисы
        services.AddScoped<ISystemRepository, Postgres.SystemRepository>();
        services.AddScoped<ITeamRepository, Postgres.TeamRepository>();
        services.AddScoped<IInviteRepository, Postgres.InviteRepository>();
        services.AddScoped<IUserRepository, Postgres.UserRepository>();
        services.AddScoped<IAvatarRepository, Postgres.AvatarRepository>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SuperSecretIntegrationTestingKey1234567890!",
                ["Jwt:Issuer"] = "AnalyzerTestIssuer",
                ["Jwt:Audience"] = "AnalyzerTestAudience",
                ["Jwt:ExpireMinutes"] = "60"
            }).Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IInviteService, InviteService>();
        services.AddScoped<IUserService, UserService>();

        ServiceProvider = services.BuildServiceProvider();
    }

    private async Task SetupTemplateAndCloneDatabaseAsync()
    {
        await using var masterConn = new NpgsqlConnection(_masterConnectionString);
        await masterConn.OpenAsync();

        // 1. Проверяем, существует ли шаблон
        await using var checkCmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @db", masterConn);
        checkCmd.Parameters.AddWithValue("db", TemplateDbName);
        var templateExists = await checkCmd.ExecuteScalarAsync() != null;

        if (!templateExists)
        {
            // Используем advisory lock, чтобы 10 разработчиков, запустивших тесты одновременно, 
            // не попытались создать шаблон все сразу.
            await using var lockCmd = new NpgsqlCommand("SELECT pg_advisory_lock(1337);", masterConn);
            await lockCmd.ExecuteNonQueryAsync();

            try
            {
                // Проверяем еще раз под локом
                templateExists = await checkCmd.ExecuteScalarAsync() != null;
                if (!templateExists)
                {
                    // Создаем пустую базу
                    await using var createCmd = new NpgsqlCommand($"CREATE DATABASE {TemplateDbName};", masterConn);
                    await createCmd.ExecuteNonQueryAsync();

                    // Накатываем миграции на шаблон
                    var templateConnStr = new NpgsqlConnectionStringBuilder(_masterConnectionString) { Database = TemplateDbName }.ConnectionString;
                    var options = new DbContextOptionsBuilder<AnalyzerDbContext>()
                        .UseNpgsql(templateConnStr).UseSnakeCaseNamingConvention().Options;
                    await using var ctx = new AnalyzerDbContext(options);
                    await ctx.Database.MigrateAsync();

                    // Обрубаем все коннекты (включая пул EF) к шаблону, иначе Postgres не даст сделать его шаблоном
                    await using var killCmd = new NpgsqlCommand($@"
                        SELECT pg_terminate_backend(pid) 
                        FROM pg_stat_activity 
                        WHERE datname = '{TemplateDbName}';", masterConn);
                    await killCmd.ExecuteNonQueryAsync();

                    // Запрещаем обычные подключения и помечаем как шаблон
                    await using var alterCmd = new NpgsqlCommand($@"
                        ALTER DATABASE {TemplateDbName} ALLOW_CONNECTIONS = false;
                        UPDATE pg_database SET datistemplate = true WHERE datname = '{TemplateDbName}';", masterConn);
                    await alterCmd.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await using var unlockCmd = new NpgsqlCommand("SELECT pg_advisory_unlock(1337);", masterConn);
                await unlockCmd.ExecuteNonQueryAsync();
            }
        }

        // 2. Создаем изолированную базу для текущего прогона из шаблона
        await using var cloneCmd = new NpgsqlCommand($"CREATE DATABASE {_testDbName} TEMPLATE {TemplateDbName};", masterConn);
        await cloneCmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        // Удаляем изолированную базу после прохождения всех тестов
        await using var masterConn = new NpgsqlConnection(_masterConnectionString);
        await masterConn.OpenAsync();

        // Принудительно отключаем возможные повисшие коннекты пула EF Core
        await using var killCmd = new NpgsqlCommand($@"
            SELECT pg_terminate_backend(pid) 
            FROM pg_stat_activity 
            WHERE datname = '{_testDbName}';", masterConn);
        await killCmd.ExecuteNonQueryAsync();

        await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {_testDbName};", masterConn);
        await dropCmd.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<SharedDatabaseFixture>
{ }