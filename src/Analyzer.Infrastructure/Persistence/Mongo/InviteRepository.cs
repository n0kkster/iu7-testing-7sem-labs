using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using MongoDB.Driver;

namespace Analyzer.Infrastructure.Persistence.Mongo;

public class InviteRepository(IMongoDatabase database) : IInviteRepository
{
    private readonly IMongoCollection<Invite> _collection = 
        database.GetCollection<Invite>("invites");

    public async Task<Invite?> GetByIdAsync(Guid id) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<Invite?> GetByCodeAsync(string code) =>
        await _collection.Find(x => x.Code == code).FirstOrDefaultAsync();

    public async Task<IReadOnlyCollection<Invite>> GetByTeamIdAsync(Guid teamId) =>
        await _collection.Find(x => x.TeamId == teamId).ToListAsync();

    public async Task AddAsync(Invite invite) =>
        await _collection.InsertOneAsync(invite);

    public async Task UpdateAsync(Invite invite) =>
        await _collection.ReplaceOneAsync(x => x.Id == invite.Id, invite);
}