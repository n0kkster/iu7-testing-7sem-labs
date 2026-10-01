using System.Text;
using Analyzer.Api.Middlewares;
using Analyzer.Application.Interfaces.Providers;
using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Application.Interfaces.Services;
using Analyzer.Application.Services;
using Analyzer.Infrastructure.Data;
using Analyzer.Infrastructure.Persistence;
using Analyzer.Infrastructure.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Neo4j.Driver;
using Serilog;

using Postgres = Analyzer.Infrastructure.Persistence.Postgres;
using Mongo = Analyzer.Infrastructure.Persistence.Mongo;

// ============================================================================
// 🔐 НАСТРОЙКА ЛОГГЕРА
// ============================================================================

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("🚀 Запуск API-сервиса FaultAnalyzer");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog();

    // ============================================================================
    // 🗄️ 1. БАЗЫ ДАННЫХ
    // ============================================================================

    var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Postgres";
    Log.Information("Используется база данных: {DbProvider}", dbProvider);

    if (dbProvider.Equals("Mongo", StringComparison.OrdinalIgnoreCase))
    {
        // --- НАСТРОЙКА MONGODB ---
        
        MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
            new MongoDB.Bson.Serialization.Serializers.GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));

        var mongoConnectionString = builder.Configuration.GetConnectionString("MongoConnection");
        var mongoClient = new MongoDB.Driver.MongoClient(mongoConnectionString);
        
        builder.Services.AddSingleton(sp => 
            mongoClient.GetDatabase("analyzer_db"));

        builder.Services.AddScoped<ISystemRepository, Mongo.SystemRepository>();
        builder.Services.AddScoped<ITeamRepository, Mongo.TeamRepository>();
        builder.Services.AddScoped<IInviteRepository, Mongo.InviteRepository>();
        builder.Services.AddScoped<IUserRepository, Mongo.UserRepository>();
        builder.Services.AddScoped<IAvatarRepository, Mongo.AvatarRepository>();
    }
    else
    {
        // --- НАСТРОЙКА POSTGRESQL + EF CORE ---
        builder.Services.AddDbContext<AnalyzerDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("PGConnection"),
                npgsqlOptions => 
                {
                    npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
            options.UseSnakeCaseNamingConvention();
        });

        builder.Services.AddScoped<ISystemRepository, Postgres.SystemRepository>();
        builder.Services.AddScoped<ITeamRepository, Postgres.TeamRepository>();
        builder.Services.AddScoped<IInviteRepository, Postgres.InviteRepository>();
        builder.Services.AddScoped<IUserRepository, Postgres.UserRepository>();
        builder.Services.AddScoped<IAvatarRepository, Postgres.AvatarRepository>();
    }

    // Neo4j
    var neo4jUri = builder.Configuration["Neo4jSettings:Uri"];
    var neo4jUser = builder.Configuration["Neo4jSettings:User"];
    var neo4jPass = builder.Configuration["Neo4jSettings:Password"];

    builder.Services.AddSingleton(sp =>
        GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPass)));

    // ============================================================================
    // 🏗️ 2. РЕПОЗИТОРИИ
    // ============================================================================

    builder.Services.AddScoped<IGraphRepository, Neo4jGraphRepository>();

    // ============================================================================
    // ⚙️ 3. БИЗНЕС-ЛОГИКА
    // ============================================================================

    builder.Services.AddScoped<IJwtProvider, JwtProvider>();
    builder.Services.AddScoped<IImageProvider, ImageSharpImageProvider>();

    builder.Services.AddScoped<IGraphService, GraphService>();
    builder.Services.AddScoped<IAnalysisService, AnalysisService>();
    builder.Services.AddScoped<ISystemService, SystemService>();
    builder.Services.AddScoped<ITeamService, TeamService>();
    builder.Services.AddScoped<IInviteService, InviteService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAvatarService, AvatarService>();

    // ============================================================================
    // 🔐 4. АУТЕНТИФИКАЦИЯ И БЕЗОПАСНОСТЬ
    // ============================================================================

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && (path.Value?.Contains("/export") ?? false))
                        context.Token = accessToken;
                    
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // Настройка CORS для Blazor-клиентов
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("BlazorClientPolicy", policy =>
        {
            policy.WithOrigins(
                    "https://localhost:1337",
                    "https://localhost:1777")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ============================================================================
    // 📖 5. API, КОНТРОЛЛЕРЫ И SWAGGER
    // ============================================================================

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "FaultAnalyzer API V1",
            Version = "v1",
            Description = "Первая версия API"
        });

        options.SwaggerDoc("v2", new OpenApiInfo
        {
            Title = "FaultAnalyzer API V2",
            Version = "v2",
            Description = "Вторая версия REST API"
        });

        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            var path = apiDesc.RelativePath;
            if (string.IsNullOrEmpty(path))
                return false;

            return path.StartsWith($"api/{docName}", StringComparison.OrdinalIgnoreCase);
        });

        options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));

        options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri("/api/v1/users/swagger-login", UriKind.Relative) 
                }
            }
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("OAuth2", document)] = []
        });
    });

    // ============================================================================
    // ❌ 6. ИСКЛЮЧЕНИЯ
    // ============================================================================

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ============================================================================
    // 🚀 BUILD & CONFIGURE APP
    // ============================================================================

    var app = builder.Build();

    // ============================================================================
    // 🔐 7. MIDDLEWARE PIPELINE
    // ============================================================================
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FaultAnalyzer API v1");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "FaultAnalyzer API v2");
            options.RoutePrefix = string.Empty;
        });
    }

    app.UseHttpsRedirection();

    // Применяем политику CORS
    app.UseCors("BlazorClientPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    // Маппинг контроллеров
    app.MapControllers();

    // ============================================================================
    // 8. Инициализация первого админа, если база пуста
    // ============================================================================

    await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration);

    // ============================================================================
    // 🚀 RUN
    // ============================================================================

    Log.Information("✅ API готов к приему запросов");
    await app.RunAsync();
}
catch (HostAbortedException)
{
    Log.Information("API был запущен при выполнении миграций, выходим..");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Критическая ошибка при запуске API");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }