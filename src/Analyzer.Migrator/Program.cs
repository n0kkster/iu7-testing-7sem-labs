using Analyzer.Migrator;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

Console.WriteLine("=======================================");
Console.WriteLine("              МИГРАТОР 1337            ");
Console.WriteLine("=======================================");


// 1. Загрузка конфигурации
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var batchSize = config.GetValue<int>("MigrationSettings:BatchSize");
var pgConn = config.GetSection("MigrationSettings:PgConnection").Value;
var mongoConn = config.GetSection("MigrationSettings:MongoConnection").Value;
var mongoDbName = config.GetSection("MigrationSettings:MongoDatabaseName").Value;

// 2. Инициализация БД 
var pgOptions = new DbContextOptionsBuilder<AnalyzerDbContext>()
    .UseNpgsql(pgConn)
    .UseSnakeCaseNamingConvention()
    .Options;

using var pgContext = new AnalyzerDbContext(pgOptions);

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
var mongoClient = new MongoClient(mongoConn);
var mongoDb = mongoClient.GetDatabase(mongoDbName);

// 3. Создание сервиса
var migrator = new MigratorService(pgContext, mongoDb, batchSize);

// 4. МЕНЮ
Console.WriteLine("\nВыберите направление миграции:");
Console.WriteLine("1. Postgres -> MongoDB");
Console.WriteLine("2. MongoDB -> Postgres");
Console.Write("Ваш выбор: ");
var choice = Console.ReadLine();

try
{
    if (choice == "1")
    {
        await migrator.MigratePgToMongoAsync<Team>("teams");
        await migrator.MigratePgToMongoAsync<User>("users");
        await migrator.MigratePgToMongoAsync<ITSystem>("systems");
        await migrator.MigratePgToMongoAsync<Invite>("invites");
        await migrator.MigratePgToMongoAsync<Avatar>("avatars");
    }
    else if (choice == "2")
    {
        await migrator.MigrateMongoToPgAsync<Team>("teams");
        await migrator.MigrateMongoToPgAsync<User>("users");
        await migrator.MigrateMongoToPgAsync<ITSystem>("systems");
        await migrator.MigrateMongoToPgAsync<Invite>("invites");
        await migrator.MigrateMongoToPgAsync<Avatar>("avatars");
    }
    else
    {
        Console.WriteLine("Неверный выбор.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ПРОИЗОШЛА КРИТИЧЕСКАЯ ОШИБКА:\n{ex.Message}");
}

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();