using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using MongoDB.Driver;

namespace Analyzer.Infrastructure.Persistence.Mongo;

public class SystemRepository(IMongoDatabase database) : ISystemRepository
{
    private readonly IMongoCollection<ITSystem> _collection = 
        database.GetCollection<ITSystem>("systems");

    public async Task<ITSystem?> GetByIdAsync(Guid systemId) =>
        await _collection.Find(x => x.Id == systemId).FirstOrDefaultAsync();

    public async Task<IReadOnlyCollection<ITSystem>> GetByTeamIdAsync(Guid teamId) =>
        await _collection.Find(x => x.TeamId == teamId).ToListAsync();

    public async Task AddAsync(ITSystem system) =>
        await _collection.InsertOneAsync(system);

    public async Task UpdateAsync(ITSystem system) =>
        await _collection.ReplaceOneAsync(x => x.Id == system.Id, system);

    public async Task DeleteAsync(Guid systemId) =>
        await _collection.DeleteOneAsync(x => x.Id == systemId);

    public async Task<bool> ExistsWithNameAsync(Guid teamId, string name) =>
        await _collection.Find(x => x.TeamId == teamId && x.Name == name).AnyAsync();
}