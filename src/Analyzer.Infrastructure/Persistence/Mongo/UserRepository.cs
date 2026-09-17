using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using MongoDB.Driver;

namespace Analyzer.Infrastructure.Persistence.Mongo;

public class UserRepository(IMongoDatabase database) : IUserRepository
{
    private readonly IMongoCollection<User> _collection = 
        database.GetCollection<User>("users");

    public async Task<User?> GetByIdAsync(Guid userId) =>
        await _collection.Find(x => x.Id == userId).FirstOrDefaultAsync();

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _collection.Find(x => x.Username == username).FirstOrDefaultAsync();

    public async Task<IReadOnlyCollection<User>> GetAllUsersAsync() =>
        await _collection.Find(_ => true).ToListAsync();

    public async Task<bool> ExistsByUsernameAsync(string username) =>
        await _collection.Find(x => x.Username == username).AnyAsync();

    public async Task<bool> ExistsByEmailAsync(string email) =>
        await _collection.Find(x => x.Email == email).AnyAsync();

    public async Task AddAsync(User user) =>
        await _collection.InsertOneAsync(user);

    public async Task UpdateAsync(User user) =>
        await _collection.ReplaceOneAsync(x => x.Id == user.Id, user);

    public async Task DeleteAsync(Guid userId) =>
        await _collection.DeleteOneAsync(x => x.Id == userId);
}