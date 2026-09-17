using Analyzer.Domain.Entities;

namespace Analyzer.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<IReadOnlyCollection<User>> GetAllUsersAsync();
    
    Task<bool> ExistsByUsernameAsync(string username); 
    Task<bool> ExistsByEmailAsync(string email); 
    
    Task AddAsync(User user);
    
    Task UpdateAsync(User user); 

    Task DeleteAsync(Guid userId);
}