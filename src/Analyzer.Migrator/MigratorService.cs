using Analyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Diagnostics;

namespace Analyzer.Migrator;

public class MigratorService(AnalyzerDbContext pgContext, IMongoDatabase mongoDb, int batchSize)
{
    private readonly AnalyzerDbContext _pgContext = pgContext;
    private readonly IMongoDatabase _mongoDb = mongoDb;
    private readonly int _batchSize = batchSize;

    // ==========================================
    // ⬇️ POSTGRES -> MONGO
    // ==========================================
    public async Task MigratePgToMongoAsync<TEntity>(string mongoCollectionName) where TEntity : class
    {
        Console.WriteLine($"\n🚀 Старт миграции {typeof(TEntity).Name}: Postgres -> Mongo...");
        var stopwatch = Stopwatch.StartNew();

        var collection = _mongoDb.GetCollection<TEntity>(mongoCollectionName);
        
        await collection.DeleteManyAsync(FilterDefinition<TEntity>.Empty);

        var batch = new List<TEntity>(_batchSize);
        long totalMigrated = 0;

        await foreach (var entity in _pgContext.Set<TEntity>().AsNoTracking().AsAsyncEnumerable())
        {
            batch.Add(entity);

            if (batch.Count >= _batchSize)
            {
                await collection.InsertManyAsync(batch);
                totalMigrated += batch.Count;
                batch.Clear();
                Console.Write($"\rОбработано: {totalMigrated}...");
            }
        }

        if (batch.Count > 0)
        {
            await collection.InsertManyAsync(batch);
            totalMigrated += batch.Count;
        }

        stopwatch.Stop();
        Console.WriteLine($"\n✅ Завершено! Перенесено: {totalMigrated} записей. Время: {stopwatch.Elapsed}");
    }

    // ==========================================
    // ⬆️ MONGO -> POSTGRES
    // ==========================================
    public async Task MigrateMongoToPgAsync<TEntity>(string mongoCollectionName) where TEntity : class
    {
        Console.WriteLine($"\n🚀 Старт миграции {typeof(TEntity).Name}: Mongo -> Postgres...");
        var stopwatch = Stopwatch.StartNew();

        var collection = _mongoDb.GetCollection<TEntity>(mongoCollectionName);
        var dbSet = _pgContext.Set<TEntity>();

        await _pgContext.Database.ExecuteSqlAsync($"TRUNCATE TABLE \"{_pgContext.Model.FindEntityType(typeof(TEntity))!.GetTableName()}\" CASCADE;");

        long totalMigrated = 0;

        var options = new FindOptions
        {
            BatchSize = _batchSize 
        };
        
        using var cursor = await collection.Find(FilterDefinition<TEntity>.Empty, options).ToCursorAsync();

        while (await cursor.MoveNextAsync())
        {
            var batch = cursor.Current.ToList();
            if (batch.Count == 0) continue;

            // Массовое добавление в EF Core
            await dbSet.AddRangeAsync(batch);
            await _pgContext.SaveChangesAsync();

            _pgContext.ChangeTracker.Clear();

            totalMigrated += batch.Count;
            Console.Write($"\rОбработано: {totalMigrated}...");
        }

        stopwatch.Stop();
        Console.WriteLine($"\n✅ Завершено! Перенесено: {totalMigrated} записей. Время: {stopwatch.Elapsed}");
    }
}