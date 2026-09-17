using Analyzer.Infrastructure.Data;
using Analyzer.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Containers;

using Postgres = Analyzer.Infrastructure.Persistence.Postgres;
using Mongo = Analyzer.Infrastructure.Persistence.Mongo;
using Serilog;

namespace Analyzer.IntegrationTests.Fixtures;

public class SharedDatabaseFixture : IAsyncLifetime
{
    private IContainer _dbContainer = null!;
    
    public string ProviderName { get; private set; } = string.Empty;
    
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        ProviderName = Environment.GetEnvironmentVariable("TEST_DB") ?? "Postgres";
        var services = new ServiceCollection();

        if (ProviderName == "Mongo")
        {
            var mongoContainer = new MongoDbBuilder("mongo:latest").Build();
            _dbContainer = mongoContainer;
            await _dbContainer.StartAsync();

            var connectionString = mongoContainer.GetConnectionString();
            
            MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
                new MongoDB.Bson.Serialization.Serializers.GuidSerializer(
                    MongoDB.Bson.GuidRepresentation.Standard));

            var mongoClient = new MongoDB.Driver.MongoClient(connectionString);
            services.AddSingleton(sp => mongoClient.GetDatabase("test_db"));

            services.AddScoped<ISystemRepository, Mongo.SystemRepository>();
            services.AddScoped<ITeamRepository, Mongo.TeamRepository>();
            services.AddScoped<IInviteRepository, Mongo.InviteRepository>();
            services.AddScoped<IUserRepository, Mongo.UserRepository>();
            services.AddScoped<IAvatarRepository, Mongo.AvatarRepository>();
        }
        else
        {
            var pgContainer = new PostgreSqlBuilder("postgres:17")
                .WithDatabase("AnalyzerTestDb")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            _dbContainer = pgContainer;
            await _dbContainer.StartAsync();

            var connectionString = pgContainer.GetConnectionString();

            services.AddDbContext<AnalyzerDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.UseSnakeCaseNamingConvention();
            });

            services.AddScoped<ISystemRepository, Postgres.SystemRepository>();
            services.AddScoped<ITeamRepository, Postgres.TeamRepository>();
            services.AddScoped<IInviteRepository, Postgres.InviteRepository>();
            services.AddScoped<IUserRepository, Postgres.UserRepository>();
            services.AddScoped<IAvatarRepository, Postgres.AvatarRepository>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AnalyzerDbContext>().Database.MigrateAsync();
        }

        ServiceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_dbContainer != null)
            await _dbContainer.DisposeAsync();
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<SharedDatabaseFixture>
{ }