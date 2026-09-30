using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Shared.DTO.Common;
using MongoDB.Driver;

namespace Analyzer.Infrastructure.Persistence.Mongo;

public class AvatarRepository: IAvatarRepository
{
    private readonly IMongoCollection<Avatar> _collection;

    public AvatarRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Avatar>("avatars");

        var indexKeysDefinition = Builders<Avatar>.IndexKeys
            .Ascending(x => x.UserId)
            .Ascending(x => x.Hash);
            
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<Avatar>(indexKeysDefinition, indexOptions);
        
        _collection.Indexes.CreateOne(indexModel);
    }

    public async Task<Avatar?> GetByIdAsync(Guid id) =>
        await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();

    public async Task<Avatar?> GetByHashAsync(Guid userId, string hash) =>
        await _collection
            .Find(x => x.UserId == userId && x.Hash == hash)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyCollection<AvatarDto>> GetHistoryByUserIdAsync(Guid userId)
    {
        var avatars = await _collection
            .Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt) 
            .ToListAsync();
        
        return avatars.Select(a => new AvatarDto(
            a.Id, a.CreatedAt, a.ContentType, a.Data
        )).ToList();
    }

    public async Task AddAsync(Avatar avatar) =>
        await _collection.InsertOneAsync(avatar);

    public async Task DeleteAsync(Guid id) =>
        await _collection.DeleteOneAsync(x => x.Id == id);
}