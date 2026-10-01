using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Infrastructure.Data;
using Analyzer.Infrastructure.Data.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Analyzer.Infrastructure.Persistence.Postgres;

public class UserRepository(AnalyzerDbContext context) : IUserRepository
{
    private readonly AnalyzerDbContext _context = context;
    public async Task<User?> GetByIdAsync(Guid userId)
    {
        return (await _context.Users.FirstOrDefaultAsync(u => u.Id == userId))?.ToDomain();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return (await _context.Users.FirstOrDefaultAsync(u => u.Username == username))?.ToDomain();
    }

    public async Task<IReadOnlyCollection<User>> GetAllUsersAsync()
    {
        return await _context.Users.Select(u => u.ToDomain()).ToListAsync();
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user.ToEntity());
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        var entity = await _context.Users.FindAsync(user.Id);
        if (entity != null)
        {
            entity.Username = user.Username;
            entity.Email = user.Email;
            entity.PasswordHash = user.PasswordHash;
            entity.Role = (int)user.Role;
            entity.TeamId = user.TeamId;
            entity.AvatarId = user.AvatarId;

            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is not null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}