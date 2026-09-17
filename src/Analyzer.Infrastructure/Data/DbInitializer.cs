using Analyzer.Application.Interfaces.Repositories;
using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Analyzer.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        
        var dbProvider = configuration["DatabaseProvider"] ?? "Postgres";

        try
        {
            if (dbProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                var context = scope.ServiceProvider.GetRequiredService<AnalyzerDbContext>();
                await context.Database.MigrateAsync();
                Log.Information("Миграции PostgreSQL успешно применены.");
            }

            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            
            var adminConfig = configuration.GetSection("AdminSettings");
            var username = adminConfig["Username"] ?? "admin";
            var email = adminConfig["Email"] ?? "admin@analyzer.local";
            
            var adminExists = await userRepository.ExistsByUsernameAsync(username);
            
            if (!adminExists)
            {
                Log.Information("Администратор не найден. Создаю дефолтного администратора...");

                var rawPassword = adminConfig["Password"] ?? "admin";
                var passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(rawPassword);
                
                var adminUser = User.CreateAdmin(username, email, passwordHash);                
                
                await userRepository.AddAsync(adminUser);

                Log.Information("Глобальный администратор успешно создан.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при инициализации базы данных.");
            throw;
        }
    }
}